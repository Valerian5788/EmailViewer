using System;
using System.Collections.Generic;
using System.Linq;
using MimeKit;
using HtmlAgilityPack;

namespace EmailViewer.Services
{
    public class EnhancedEmailService
    {
        public class EmailThread
        {
            public List<EmailContent> Messages { get; set; } = new List<EmailContent>();
        }

        public class EmailContent
        {
            public string MessageId { get; set; }
            public string InReplyTo { get; set; }
            public string From { get; set; }
            public string Subject { get; set; }
            public string Date { get; set; }
            public string HtmlBody { get; set; }
            public string TextBody { get; set; }
            public string FilePath { get; set; }
        }

        public EmailThread LoadEmailThread(string filePath)
        {
            var thread = new EmailThread();
            var currentMessage = MimeMessage.Load(filePath);

            while (currentMessage != null)
            {
                thread.Messages.Add(ConvertToEmailContent(currentMessage, filePath));

                // For now, we'll just load a single email
                // In a real implementation, you'd need to find and load previous emails in the thread
                currentMessage = null;
            }

            thread.Messages.Reverse(); // Oldest message first
            return thread;
        }

        private EmailContent ConvertToEmailContent(MimeMessage message, string filePath)
        {
            return new EmailContent
            {
                MessageId = message.MessageId,
                InReplyTo = message.InReplyTo,
                From = message.From.ToString(),
                Subject = message.Subject,
                Date = message.Date.ToString("g"),
                HtmlBody = message.HtmlBody,
                TextBody = message.TextBody,
                FilePath = filePath
            };
        }

        public string SanitizeHtml(string html)
        {
            if (string.IsNullOrEmpty(html))
                return string.Empty;

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Remove potentially dangerous elements and attributes
            var nodesToRemove = doc.DocumentNode.SelectNodes("//script|//iframe|//object|//embed");
            if (nodesToRemove != null)
            {
                foreach (var node in nodesToRemove)
                {
                    node.Remove();
                }
            }

            // Remove on* attributes (e.g., onclick, onload)
            var nodesWithOnAttributes = doc.DocumentNode.SelectNodes("//*[@*[starts-with(name(), 'on')]]");
            if (nodesWithOnAttributes != null)
            {
                foreach (var node in nodesWithOnAttributes)
                {
                    foreach (var attribute in node.Attributes.ToList())
                    {
                        if (attribute.Name.StartsWith("on", StringComparison.OrdinalIgnoreCase))
                        {
                            node.Attributes.Remove(attribute);
                        }
                    }
                }
            }

            return doc.DocumentNode.OuterHtml;
        }
    }
}