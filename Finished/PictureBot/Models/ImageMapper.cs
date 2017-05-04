using Microsoft.Azure.Search.Models;

namespace PictureBot.Models
{
    public class ImageMapper 
    {
        public static SearchHit ToSearchHit(SearchResult hit)
        {
            var searchHit = new SearchHit
            {
                Key = (string)hit.Document["rid"],
                Title = (string)hit.Document["FileName"],
                PictureUrl = (string)hit.Document["BlobUri"],
                Description = (string)hit.Document["Caption"]
            };

            object Tags;
            if (hit.Document.TryGetValue("Tags", out Tags))
            {
                searchHit.PropertyBag.Add("Tags", Tags);
            }

            object NumFaces;
            if (hit.Document.TryGetValue("NumFaces", out NumFaces))
            {
                searchHit.PropertyBag.Add("NumFaces", NumFaces);
            }

            object Faces;
            if (hit.Document.TryGetValue("Faces", out Faces))
            {
                searchHit.PropertyBag.Add("Faces", Faces);
            }

            return searchHit;
        }

    }
}