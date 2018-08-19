using System.Collections.Generic;

namespace SwaggerToCsv.Models
{
    public class SwaggerOperation
    {
        public List<string> Tags { get; set; }
        public string Summary { get; set; }
        public string Description { get; set; }
    }
}