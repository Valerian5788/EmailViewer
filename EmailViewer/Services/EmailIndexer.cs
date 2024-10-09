using System;
using System.IO;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.QueryParsers.Classic;
using EmailViewer.Helpers;

namespace EmailViewer.Services
{
    public class EmailIndexer : IDisposable
    {
        private const LuceneVersion AppLuceneVersion = LuceneVersion.LUCENE_48;
        private readonly string _indexDir;
        private IndexWriter _writer;
        private FSDirectory _directory;

        public EmailIndexer(string indexDir = null)
        {
            _indexDir = indexDir ?? Path.Combine(Path.GetTempPath(), "email_index");
            InitializeIndexWriter(true);
        }

        private void InitializeIndexWriter(bool create)
        {
            _directory = FSDirectory.Open(_indexDir);
            var analyzer = new StandardAnalyzer(AppLuceneVersion);
            var config = new IndexWriterConfig(AppLuceneVersion, analyzer)
            {
                OpenMode = create ? OpenMode.CREATE : OpenMode.CREATE_OR_APPEND
            };

            if (IndexWriter.IsLocked(_directory))
            {
                Logger.Log("Stale lock detected. Attempting to clear...");
                IndexWriter.Unlock(_directory);
            }

            int retries = 3;
            while (retries > 0)
            {
                try
                {
                    _writer = new IndexWriter(_directory, config);
                    break;
                }
                catch (LockObtainFailedException)
                {
                    retries--;
                    if (retries == 0)
                    {
                        throw;
                    }
                    System.Threading.Thread.Sleep(1000);
                }
            }
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

            _writer.AddDocument(doc);
            _writer.Commit();
        }

        public SearchResult[] Search(string searchTerm, int maxResults = 10)
        {
            using (var reader = _writer.GetReader(true))
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

        public void Dispose()
        {
            _writer?.Dispose();
            _directory?.Dispose();
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