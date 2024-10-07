using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Newtonsoft.Json;
using EmailViewer.Models;

namespace EmailViewer.Services
{
    public class NoteManager
    {
        private const string NotesFile = "notes.json";
        private List<Note> _notes;

        public NoteManager()
        {
            _notes = LoadNotes();
        }

        public Note AddNote(string emailPath, string title, string content, List<string> tags)
        {
            var note = new Note
            {
                Id = _notes.Count > 0 ? _notes.Max(n => n.Id) + 1 : 1,
                EmailPath = emailPath,
                Title = title,
                Content = content,
                Tags = tags ?? new List<string>(),
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _notes.Add(note);
            SaveNotes();
            return note;
        }

        public Note UpdateNote(int id, string title, string content, List<string> tags)
        {
            var note = _notes.FirstOrDefault(n => n.Id == id);
            if (note != null)
            {
                note.Title = title;
                note.Content = content;
                note.Tags = tags ?? new List<string>();
                note.UpdatedAt = DateTime.Now;
                SaveNotes();
            }
            return note;
        }

        public void DeleteNote(int id)
        {
            _notes.RemoveAll(n => n.Id == id);
            SaveNotes();
        }

        public List<Note> GetNotesForEmail(string emailPath)
        {
            return _notes.Where(n => n.EmailPath == emailPath).ToList();
        }

        public List<Note> SearchNotes(string searchTerm, List<string> tags = null)
        {
            return _notes.Where(n =>
                (string.IsNullOrEmpty(searchTerm) ||
                 n.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                 n.Content.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) &&
                (tags == null || tags.Count == 0 || tags.All(t => n.Tags.Contains(t)))
            ).ToList();
        }

        private List<Note> LoadNotes()
        {
            if (File.Exists(NotesFile))
            {
                string json = File.ReadAllText(NotesFile);
                return JsonConvert.DeserializeObject<List<Note>>(json) ?? new List<Note>();
            }
            return new List<Note>();
        }

        private void SaveNotes()
        {
            string json = JsonConvert.SerializeObject(_notes, Formatting.Indented);
            File.WriteAllText(NotesFile, json);
        }
    }
}