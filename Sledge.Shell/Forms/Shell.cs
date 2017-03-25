﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using LogicAndTrick.Oy;
using Sledge.Common.Documents;
using Sledge.Common.Settings;
using Sledge.Shell.Controls;

namespace Sledge.Shell.Forms
{
    public partial class Shell : BaseForm
    {
        private readonly List<IDocument> _documents;
        private readonly object _lock = new object();

        public Shell()
        {
            _documents = new List<IDocument>();

            InitializeComponent();
            InitializeShell();
        }

        private void InitializeShell()
        {
            DocumentTabs.TabPages.Clear();
            
            Oy.Subscribe<string>("Context:Added", ContextAdded);
            Oy.Subscribe<string>("Context:Removed", ContextRemoved);

            Oy.Subscribe<IDocument>("Document:Opened", OpenDocument);
            Oy.Subscribe<IDocument>("Document:Closed", CloseDocument);

            Oy.Subscribe<string>("Shell:OpenCommandBox", OpenCommandBox);
        }

        protected override void OnLoad(EventArgs e)
        {
            Bootstrapping.Startup(this).ContinueWith(Bootstrapping.Initialise);
            Closing += DoClosing;
        }

        private async void DoClosing(object sender, CancelEventArgs e)
        {
            // Close all the open documents
            foreach (var doc in _documents.ToArray())
            {
                await Oy.Publish("Document:RequestClose", doc);
                if (_documents.Contains(doc))
                {
                    e.Cancel = true;
                    return;
                }
            }

            // Close anything else
            if (!await Bootstrapping.ShuttingDown())
            {
                e.Cancel = true;
                return;
            }

            Closing -= DoClosing;
            Enabled = false;
            e.Cancel = true;
            await Bootstrapping.Shutdown();
            Close();
        }

        internal IEnumerable<DockedPanel> GetDockPanels()
        {
            yield return LeftSidebar;
            yield return RightSidebar;
            yield return BottomSidebar;
        }
        
        // Subscriptions

        private async Task OpenDocument(IDocument document)
        {
            lock (_lock)
            {
                if (_documents.Contains(document)) return;
                _documents.Add(document);
                document.PropertyChanged += DocumentNameChanged;
                DocumentTabs.TabPages.Add(new TabPage { Text = document.Name, Tag = document });
                TabChanged(DocumentTabs, EventArgs.Empty);
            }
        }

        private async Task CloseDocument(IDocument document)
        {
            lock (_lock)
            {
                if (!_documents.Contains(document)) return;
                _documents.Remove(document);
                document.PropertyChanged -= DocumentNameChanged;
                var page = DocumentTabs.TabPages.OfType<TabPage>().FirstOrDefault(x => x.Tag == document);
                if (page != null) DocumentTabs.TabPages.Remove(page);
                TabChanged(DocumentTabs, EventArgs.Empty);
            }
        }

        private void DocumentNameChanged(object sender, PropertyChangedEventArgs e)
        {
            var doc = sender as IDocument;
            var page = DocumentTabs.TabPages.OfType<TabPage>().FirstOrDefault(x => x.Tag == doc);
            if (page != null && doc != null) page.Text = doc.Name;
        }

        private async Task ContextAdded(string context)
        {

        }

        private async Task ContextRemoved(string context)
        {

        }

        private async Task OpenCommandBox(string obj)
        {
            var cb = new CommandBox();
            cb.Location = new Point(Location.X + (Size.Width - cb.Width) / 2, Location.Y + (Size.Height - cb.Height) / 4);
            cb.StartPosition = FormStartPosition.Manual;
            cb.Show(this);
        }

        private void TabChanged(object sender, EventArgs e)
        {
            if (DocumentTabs.SelectedTab != null)
            {
                var doc = DocumentTabs.SelectedTab.Tag as IDocument;
                if (doc != null)
                {
                    var currentControl = DocumentContainer.Controls.OfType<Control>().FirstOrDefault();
                    if (currentControl != doc.Control)
                    {
                        DocumentContainer.Controls.Clear();
                        DocumentContainer.Controls.Add((Control) doc.Control);
                        DocumentContainer.Controls[0].Dock = DockStyle.Fill;
                    }
                    Oy.Publish("Document:Activated", doc);
                }
            }
            else
            {
                DocumentContainer.Controls.Clear();
            }
        }

        private void RequestClose(object sender, int index)
        {
            var doc = DocumentTabs.TabPages[index].Tag as IDocument;
            if (doc != null)
            {
                Oy.Publish("Document:RequestClose", doc);
            }
        }
    }
}
