using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using EmailViewer.Models;
using EmailViewer.Services;

namespace EmailViewer
{
    public partial class NoteWindow : Window
    {
        private NoteManager noteManager;
        private string emailPath;
        private Note currentNote;
        private ObservableCollection<string> availableTags;
        private ObservableCollection<string> selectedTags;

        public NoteWindow(string emailPath, NoteManager noteManager, Note note = null)
        {
            InitializeComponent();
            this.emailPath = emailPath;
            this.noteManager = noteManager;
            availableTags = new ObservableCollection<string> { "Urgent", "To Do", "To Treat" };
            selectedTags = new ObservableCollection<string>();
            noteTagsComboBox.ItemsSource = availableTags;
            selectedTagsItemsControl.ItemsSource = selectedTags;

            if (note != null)
            {
                currentNote = note;
                noteTitleTextBox.Text = note.Title;
                noteContentTextBox.Text = note.Content;
                foreach (var tag in note.Tags)
                {
                    selectedTags.Add(tag);
                }
            }
            else
            {
                DeleteButton.Visibility = Visibility.Collapsed;
            }
        }

        private void AddTagButton_Click(object sender, RoutedEventArgs e)
        {
            string newTag = noteTagsComboBox.Text.Trim();
            if (!string.IsNullOrEmpty(newTag) && !selectedTags.Contains(newTag))
            {
                selectedTags.Add(newTag);
                if (!availableTags.Contains(newTag))
                {
                    availableTags.Add(newTag);
                }
                noteTagsComboBox.Text = "";
            }
        }

        private void RemoveTagButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tag)
            {
                selectedTags.Remove(tag);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string title = noteTitleTextBox.Text;
            string content = noteContentTextBox.Text;
            List<string> tags = new List<string>(selectedTags);

            if (currentNote != null)
            {
                noteManager.UpdateNote(currentNote.Id, title, content, tags);
            }
            else
            {
                noteManager.AddNote(emailPath, title, content, tags);
            }

            DialogResult = true;
            Close();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentNote != null)
            {
                noteManager.DeleteNote(currentNote.Id);
                DialogResult = true;
                Close();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}