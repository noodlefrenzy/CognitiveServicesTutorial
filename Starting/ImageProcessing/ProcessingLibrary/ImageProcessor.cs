using Microsoft.ProjectOxford.Common.Contract;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using Microsoft.ProjectOxford.Vision;
using ServiceHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ImageProcessingLibrary
{
    public class ImageProcessor
    {
        private static FaceAttributeType[] DefaultFaceAttributeTypes = new FaceAttributeType[] { FaceAttributeType.Age, FaceAttributeType.Gender };
        private static VisualFeature[] DefaultVisualFeatureTypes = new VisualFeature[] { VisualFeature.Tags, VisualFeature.Description };

        public static async Task<ImageInsights> ProcessImageAsync(Func<Task<Stream>> imageStreamCallback, string imageId)
        {
            ImageInsights result = new ImageInsights { ImageId = imageId };

            // trigger computer vision, face and emotion analysis
            List<Emotion> emotionResult = new List<Emotion>();
            await Task.WhenAll(AnalyzeImageFeaturesAsync(imageStreamCallback, result), AnalyzeFacesAsync(imageStreamCallback, result), AnalyzeEmotionAsync(imageStreamCallback, emotionResult));

            // Combine emotion and face results based on face rectangle location/size similarity
            foreach (var faceInsights in result.FaceInsights)
            {
                Emotion faceEmotion = CoreUtil.FindFaceClosestToRegion(emotionResult, faceInsights.FaceRectangle);
                if (faceEmotion != null)
                {
                    faceInsights.TopEmotion = faceEmotion.Scores.ToRankedList().First().Key;
                }
            }

            return result;
        }

        private static async Task AnalyzeImageFeaturesAsync(Func<Task<Stream>> imageStreamCallback, ImageInsights result)
        {
            var imageAnalysisResult = await VisionServiceHelper.AnalyzeImageAsync(imageStreamCallback, DefaultVisualFeatureTypes);

            result.VisionInsights = new VisionInsights
                {
                    Caption = imageAnalysisResult.Description.Captions[0].Text,
                    Tags = imageAnalysisResult.Tags.Select(t => t.Name).ToArray()
                };
        }

        private static async Task AnalyzeFacesAsync(Func<Task<Stream>> imageStreamCallback, ImageInsights result)
        {
            var faces = await FaceServiceHelper.DetectAsync(imageStreamCallback, returnFaceId: true, returnFaceLandmarks: false, returnFaceAttributes: DefaultFaceAttributeTypes);

            List<FaceInsights> faceInsightsList = new List<FaceInsights>();
            foreach (Face detectedFace in faces)
            {
                FaceInsights faceInsights = new FaceInsights
                {
                    FaceRectangle = detectedFace.FaceRectangle,
                    Age = detectedFace.FaceAttributes.Age,
                    Gender = detectedFace.FaceAttributes.Gender
                };

                SimilarPersistedFace similarPersistedFace = await FaceListManager.FindSimilarPersistedFaceAsync(imageStreamCallback, detectedFace.FaceId, detectedFace);
                if (similarPersistedFace != null)
                {
                    faceInsights.UniqueFaceId = similarPersistedFace.PersistedFaceId;
                }

                faceInsightsList.Add(faceInsights);
            }

            result.FaceInsights = faceInsightsList.ToArray();
        }

        private static async Task AnalyzeEmotionAsync(Func<Task<Stream>> imageStreamCallback, List<Emotion> faceEmotions)
        {
            faceEmotions.AddRange(await EmotionServiceHelper.RecognizeAsync(imageStreamCallback));
        }
    }
}
