using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using EmailViewer.Models;
using EmailViewer.Services;
using Newtonsoft.Json;
using EmailViewer.Tests.Helpers;

namespace EmailViewer.Tests.Services
{
    public class NoteManagerTests : IDisposable
    {
        private const string TestNotesFile = "test_notes.json";
        private NoteManager _noteManager;
        private string _originalContent;

        public NoteManagerTests()
        {
            LoggerTest.ClearLog();
            LoggerTest.Log("NoteManagerTests constructor started");

            string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, TestNotesFile);
            LoggerTest.Log($"Full path of test notes file: {fullPath}");

            if (File.Exists(fullPath))
            {
                _originalContent = File.ReadAllText(fullPath);
                LoggerTest.Log("Original content of test notes file backed up");
            }
            else
            {
                LoggerTest.Log("Test notes file does not exist");
            }

            // Start each test with an empty notes file
            File.WriteAllText(fullPath, "[]");
            LoggerTest.Log("Empty array written to test notes file");

            _noteManager = new NoteManager(fullPath);
            LoggerTest.Log("NoteManager instance created");

            LoggerTest.Log($"Test setup complete. File exists: {File.Exists(fullPath)}");
            LoggerTest.Log($"File content: {File.ReadAllText(fullPath)}");
        }

        public void Dispose()
        {
            LoggerTest.Log("Dispose method called");

            string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, TestNotesFile);

            if (_originalContent != null)
            {
                File.WriteAllText(fullPath, _originalContent);
                LoggerTest.Log("Original content restored to test notes file");
            }
            else
            {
                File.Delete(fullPath);
                LoggerTest.Log("Test notes file deleted");
            }

            LoggerTest.Log($"Dispose complete. File exists: {File.Exists(fullPath)}");
            if (File.Exists(fullPath))
            {
                LoggerTest.Log($"File content: {File.ReadAllText(fullPath)}");
            }
        }

        [Fact]
        public void AddNote_ShouldCreateNewNote()
        {
            LoggerTest.Log("AddNote_ShouldCreateNewNote test started");

            // Arrange
            string emailPath = "test@example.com";
            string title = "Test Note";
            string content = "This is a test note";
            List<string> tags = new List<string> { "Test", "Important" };

            // Act
            var addedNote = _noteManager.AddNote(emailPath, title, content, tags);
            LoggerTest.Log($"Note added: {JsonConvert.SerializeObject(addedNote)}");

            // Assert
            Assert.NotNull(addedNote);
            Assert.Equal(1, addedNote.Id);
            Assert.Equal(emailPath, addedNote.EmailPath);
            Assert.Equal(title, addedNote.Title);
            Assert.Equal(content, addedNote.Content);
            Assert.Equal(tags, addedNote.Tags);
            Assert.Equal(DateTime.Now.Date, addedNote.CreatedAt.Date);
            Assert.Equal(DateTime.Now.Date, addedNote.UpdatedAt.Date);

            // Verify the note was actually saved
            var loadedNotes = _noteManager.GetNotesForEmail(emailPath);
            LoggerTest.Log($"Loaded notes: {JsonConvert.SerializeObject(loadedNotes)}");

            Assert.Single(loadedNotes);
            Assert.Equal(addedNote.Id, loadedNotes[0].Id);

            LoggerTest.Log("AddNote_ShouldCreateNewNote test completed");
        }

        [Fact]
        public void UpdateNote_ShouldModifyExistingNote()
        {
            LoggerTest.Log("UpdateNote_ShouldModifyExistingNote test started");

            // Arrange
            var originalNote = _noteManager.AddNote("test@example.com", "Original Title", "Original Content", new List<string> { "Original" });
            int noteIdToUpdate = originalNote.Id;
            string newTitle = "Updated Title";
            string newContent = "Updated Content";
            List<string> newTags = new List<string> { "Updated", "Important" };

            // Act
            var updatedNote = _noteManager.UpdateNote(noteIdToUpdate, newTitle, newContent, newTags);
            LoggerTest.Log($"Updated note: {JsonConvert.SerializeObject(updatedNote)}");

            // Assert
            Assert.NotNull(updatedNote);
            Assert.Equal(noteIdToUpdate, updatedNote.Id);
            Assert.Equal(newTitle, updatedNote.Title);
            Assert.Equal(newContent, updatedNote.Content);
            Assert.Equal(newTags, updatedNote.Tags);
            Assert.True(updatedNote.UpdatedAt > updatedNote.CreatedAt);

            // Verify the note was actually updated in storage
            var loadedNote = _noteManager.GetNotesForEmail("test@example.com").FirstOrDefault(n => n.Id == noteIdToUpdate);
            Assert.NotNull(loadedNote);
            Assert.Equal(newTitle, loadedNote.Title);

            LoggerTest.Log("UpdateNote_ShouldModifyExistingNote test completed");
        }

        [Fact]
        public void DeleteNote_ShouldRemoveExistingNote()
        {
            LoggerTest.Log("DeleteNote_ShouldRemoveExistingNote test started");

            // Arrange
            var note = _noteManager.AddNote("test@example.com", "Test Note", "Content", new List<string> { "Test" });
            int noteIdToDelete = note.Id;

            // Act
            _noteManager.DeleteNote(noteIdToDelete);
            LoggerTest.Log($"Deleted note with ID: {noteIdToDelete}");

            // Assert
            var remainingNotes = _noteManager.GetNotesForEmail("test@example.com");
            LoggerTest.Log($"Remaining notes: {JsonConvert.SerializeObject(remainingNotes)}");
            Assert.Empty(remainingNotes);
            Assert.DoesNotContain(remainingNotes, n => n.Id == noteIdToDelete);

            LoggerTest.Log("DeleteNote_ShouldRemoveExistingNote test completed");
        }

        [Fact]
        public void GetNotesForEmail_ShouldReturnCorrectNotes()
        {
            LoggerTest.Log("GetNotesForEmail_ShouldReturnCorrectNotes test started");

            // Arrange
            string emailPath = "test@example.com";
            _noteManager.AddNote(emailPath, "Note 1", "Content 1", new List<string>());
            _noteManager.AddNote(emailPath, "Note 2", "Content 2", new List<string>());
            _noteManager.AddNote("other@example.com", "Note 3", "Content 3", new List<string>());

            // Act
            var notes = _noteManager.GetNotesForEmail(emailPath);
            LoggerTest.Log($"Notes retrieved: {JsonConvert.SerializeObject(notes)}");

            // Assert
            Assert.Equal(2, notes.Count);
            Assert.All(notes, note => Assert.Equal(emailPath, note.EmailPath));

            LoggerTest.Log("GetNotesForEmail_ShouldReturnCorrectNotes test completed");
        }

        [Fact]
        public void SearchNotes_ShouldReturnMatchingNotes()
        {
            LoggerTest.Log("SearchNotes_ShouldReturnMatchingNotes test started");

            // Arrange
            _noteManager.AddNote("test@example.com", "Important Meeting", "Discuss project timeline", new List<string> { "Urgent", "Meeting" });
            _noteManager.AddNote("test@example.com", "Todo List", "1. Buy groceries 2. Call John", new List<string> { "Todo" });
            _noteManager.AddNote("test@example.com", "Random Note", "Just a random note", new List<string>());

            // Act
            var searchResults = _noteManager.SearchNotes("meeting", new List<string> { "Urgent" });
            LoggerTest.Log($"Search results: {JsonConvert.SerializeObject(searchResults)}");

            // Assert
            Assert.Single(searchResults);
            Assert.Equal("Important Meeting", searchResults[0].Title);

            LoggerTest.Log("SearchNotes_ShouldReturnMatchingNotes test completed");
        }

        [Fact]
        public void AddNote_WithNullTags_ShouldCreateNoteWithEmptyTagList()
        {
            LoggerTest.Log("AddNote_WithNullTags_ShouldCreateNoteWithEmptyTagList test started");

            // Arrange
            string emailPath = "test@example.com";
            string title = "Test Note";
            string content = "This is a test note";

            // Act
            var addedNote = _noteManager.AddNote(emailPath, title, content, null);
            LoggerTest.Log($"Added note: {JsonConvert.SerializeObject(addedNote)}");

            // Assert
            Assert.NotNull(addedNote);
            Assert.Empty(addedNote.Tags);

            LoggerTest.Log("AddNote_WithNullTags_ShouldCreateNoteWithEmptyTagList test completed");
        }

        [Fact]
        public void UpdateNote_WithNonExistentId_ShouldReturnNull()
        {
            LoggerTest.Log("UpdateNote_WithNonExistentId_ShouldReturnNull test started");

            // Arrange
            int nonExistentId = 9999;

            // Act
            var result = _noteManager.UpdateNote(nonExistentId, "Title", "Content", new List<string>());
            LoggerTest.Log($"Update result: {result}");

            // Assert
            Assert.Null(result);

            LoggerTest.Log("UpdateNote_WithNonExistentId_ShouldReturnNull test completed");
        }

        [Fact]
        public void SearchNotes_WithEmptySearchTermAndNoTags_ShouldReturnAllNotes()
        {
            LoggerTest.Log("SearchNotes_WithEmptySearchTermAndNoTags_ShouldReturnAllNotes test started");

            // Arrange
            _noteManager.AddNote("test1@example.com", "Note 1", "Content 1", new List<string>());
            _noteManager.AddNote("test2@example.com", "Note 2", "Content 2", new List<string>());

            // Act
            var result = _noteManager.SearchNotes("", null);
            LoggerTest.Log($"Search results: {JsonConvert.SerializeObject(result)}");

            // Assert
            Assert.Equal(2, result.Count);

            LoggerTest.Log("SearchNotes_WithEmptySearchTermAndNoTags_ShouldReturnAllNotes test completed");
        }
    }
}