﻿using Microsoft.ProjectOxford.Common;
using Microsoft.ProjectOxford.Common.Contract;
using Microsoft.ProjectOxford.Emotion;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace ServiceHelpers
{
    public class EmotionData
    {
        public string EmotionName { get; set; }
        public float EmotionScore { get; set; }
    }

    public static class EmotionServiceHelper
    {
        public static int RetryCountOnQuotaLimitError = 6;
        public static int RetryDelayOnQuotaLimitError = 500;

        private static EmotionServiceClient emotionClient { get; set; }

        static EmotionServiceHelper()
        {
            InitializeEmotionService();
        }

        public static Action Throttled;

        private static string apiKey;
        public static string ApiKey {
            get{ return apiKey; }
            set {
                var changed = apiKey != value;
                apiKey = value;
                if (changed)
                    InitializeEmotionService();
            }
        }

        private static void InitializeEmotionService()
        {
            emotionClient = new EmotionServiceClient(apiKey);
        }

        private static async Task<TResponse> RunTaskWithAutoRetryOnQuotaLimitExceededError<TResponse>(Func<Task<TResponse>> action)
        {
            int retriesLeft = FaceServiceHelper.RetryCountOnQuotaLimitError;
            int delay = FaceServiceHelper.RetryDelayOnQuotaLimitError;

            TResponse response = default(TResponse);

            while (true)
            {
                try
                {
                    response = await action();
                    break;
                }
                catch (ClientException exception) when (exception.HttpStatus == (System.Net.HttpStatusCode)429 && retriesLeft > 0)
                {
                    ErrorTrackingHelper.TrackException(exception, "Emotion API throttling error");
                    if (retriesLeft == 1 && Throttled != null)
                    {
                        Throttled();
                    }

                    await Task.Delay(delay);
                    retriesLeft--;
                    delay *= 2;
                    continue;
                }
            }

            return response;
        }

        private static async Task RunTaskWithAutoRetryOnQuotaLimitExceededError(Func<Task> action)
        {
            await RunTaskWithAutoRetryOnQuotaLimitExceededError<object>(async () => { await action(); return null; } );
        }

        public static async Task<Emotion[]> RecognizeAsync(Func<Task<Stream>> imageStreamCallback)
        {
            return await RunTaskWithAutoRetryOnQuotaLimitExceededError<Emotion[]>(async () => await emotionClient.RecognizeAsync(await imageStreamCallback()));
        }

        public static async Task<Emotion[]> RecognizeAsync(string url)
        {
            return await RunTaskWithAutoRetryOnQuotaLimitExceededError<Emotion[]>(async () => await emotionClient.RecognizeAsync(url));
        }

        public static IEnumerable<EmotionData> ScoresToEmotionData(EmotionScores scores)
        {
            List<EmotionData> result = new List<EmotionData>();
            result.Add(new EmotionData { EmotionName = "Anger", EmotionScore = scores.Anger });
            result.Add(new EmotionData { EmotionName = "Contempt", EmotionScore = scores.Contempt });
            result.Add(new EmotionData { EmotionName = "Disgust", EmotionScore = scores.Disgust });
            result.Add(new EmotionData { EmotionName = "Fear", EmotionScore = scores.Fear });
            result.Add(new EmotionData { EmotionName = "Happiness", EmotionScore = scores.Happiness });
            result.Add(new EmotionData { EmotionName = "Neutral", EmotionScore = scores.Neutral });
            result.Add(new EmotionData { EmotionName = "Sadness", EmotionScore = scores.Sadness });
            result.Add(new EmotionData { EmotionName = "Surprise", EmotionScore = scores.Surprise });

            return result;
        }
    }
}
