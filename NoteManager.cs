using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Newtonsoft.Json;

namespace EmailViewer
{
    public class NoteManager
    {
        private const string NotesFile = "notes.json";
        private List<Note> _notes;

        public NoteManager()
        {
            _notes = LoadNotes();
        }

        public Note AddNote(string emailPath, string content, List<string> tags)
        {
            var note = new Note
            {
                Id = _notes.Count > 0 ? _notes.Max(n => n.Id) + 1 : 1,
                EmailPath = emailPath,
                Content = content,
                Tags = tags,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _notes.Add(note);
            SaveNotes();
            return note;
        }

        public Note UpdateNote(int id, string content, List<string> tags)
        {
            var note = _notes.FirstOrDefault(n => n.Id == id);
            if (note != null)
            {
                note.Content = content;
                note.Tags = tags;
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
                (string.IsNullOrEmpty(searchTerm) || n.Content.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) &&
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
            string json = JsonConvert.SerializeObject(_notes);
            File.WriteAllText(NotesFile, json);
        }
    }
}