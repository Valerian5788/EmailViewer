using System;
using System.IO;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.QueryParsers.Classic;

namespace EmailViewer
{
    public class EmailIndexer
    {
        private const LuceneVersion AppLuceneVersion = LuceneVersion.LUCENE_48;
        private const string INDEX_DIR = "email_index";
        private IndexWriter writer;
        private FSDirectory directory;

        public EmailIndexer()
        {
            directory = FSDirectory.Open(INDEX_DIR);
            var analyzer = new StandardAnalyzer(AppLuceneVersion);
            var config = new IndexWriterConfig(AppLuceneVersion, analyzer);
            writer = new IndexWriter(directory, config);
        }

        public void IndexEmail(string filePath, string subject, string sender, string body, DateTime date)
        {
            var doc = new Document
            {
                new StringField("path", filePath, Field.Store.YES),
                new TextField("subject", subject, Field.Store.YES),
                new StringField("sender", sender, Field.Store.YES),
                new TextField("body", body, Field.Store.YES),
                new Int64Field("date", date.Ticks, Field.Store.YES)
            };

            writer.AddDocument(doc);
            writer.Commit();
        }

        public SearchResult[] Search(string searchTerm, int maxResults = 10)
        {
            using (var reader = writer.GetReader(true))
            {
                var searcher = new IndexSearcher(reader);
                var parser = new MultiFieldQueryParser(AppLuceneVersion, new[] { "subject", "body" }, new StandardAnalyzer(AppLuceneVersion));
                var query = parser.Parse(searchTerm);

                var hits = searcher.Search(query, maxResults).ScoreDocs;
                var results = new SearchResult[hits.Length];

                for (int i = 0; i < hits.Length; i++)
                {
                    var doc = searcher.Doc(hits[i].Doc);
                    results[i] = new SearchResult
                    {
                        FilePath = doc.Get("path"),
                        Subject = doc.Get("subject"),
                        Sender = doc.Get("sender"),
                        Date = new DateTime(long.Parse(doc.Get("date")))
                    };
                }

                return results;
            }
        }

        public void Close()
        {
            writer.Dispose();
            directory.Dispose();
        }
    }

    public class SearchResult
    {
        public string FilePath { get; set; }
        public string Subject { get; set; }
        public string Sender { get; set; }
        public DateTime Date { get; set; }
    }
}