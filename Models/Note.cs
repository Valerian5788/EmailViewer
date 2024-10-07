using System;
using System.Collections.Generic;

namespace EmailViewer.Models
{
    public class Note
    {
        public int Id { get; set; }
        public string EmailPath { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public string TagsString => string.Join(", ", Tags);
    }
}