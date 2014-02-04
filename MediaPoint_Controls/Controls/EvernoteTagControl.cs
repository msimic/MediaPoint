using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using MediaPoint.Common.Interfaces;

namespace MediaPoint.Controls
{
    
    [TemplatePart(Name = "PART_CreateTagButton", Type = typeof(Button))]
    public class EvernoteTagControl : ListBox
    {
        public event EventHandler<EvernoteTagEventArgs> TagClick;
        public event EventHandler<EvernoteTagEventArgs> TagAdded;
        public event EventHandler<EvernoteTagEventArgs> TagRemoved;
        private List<ITag> _originalAllTags;
 
        static EvernoteTagControl()
        {
            // lookless control, get default style from generic.xaml
            DefaultStyleKeyProperty.OverrideMetadata(typeof(EvernoteTagControl), new FrameworkPropertyMetadata(typeof(EvernoteTagControl)));
        }

        public EvernoteTagControl()
        {
            //// some dummy data, this needs to be provided by user
            //this.ItemsSource = new List<EvernoteTagItem>() { new EvernoteTagItem("receipt"), new EvernoteTagItem("restaurant") };
            //this.AllTags = new List<string>() { "recipe", "red" };
        }

        // AllTags
        public List<ITag> AllTags
        {
            get
            {
                return (List<ITag>)GetValue(AllTagsProperty);
            }
            set { SetValue(AllTagsProperty, value); }
        }
        public static readonly DependencyProperty AllTagsProperty = DependencyProperty.Register("AllTags", typeof(List<ITag>), typeof(EvernoteTagControl), new PropertyMetadata(new List<ITag>(), PropertyChangedCallback));

        // SelectedTags
        public List<ITag> SelectedTags
        {
            get
            {
                return (List<ITag>)GetValue(SelectedTagsProperty);
            }
            set { SetValue(SelectedTagsProperty, value); }
        }
        public static readonly DependencyProperty SelectedTagsProperty = DependencyProperty.Register("SelectedTags", typeof(List<ITag>), typeof(EvernoteTagControl), new PropertyMetadata(new List<ITag>(), PropertySelectedChangedCallback));

        private static void PropertySelectedChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            var me = dependencyObject as EvernoteTagControl;
            if (me.ItemsSource == null)
                me.ItemsSource = new List<EvernoteTagItem>();

            foreach (var v in e.NewValue as List<ITag>)
            {
                (me.ItemsSource as List<EvernoteTagItem>).Add(new EvernoteTagItem() {DataContext = v, Text = v.Id});
            }
        }

        private static void PropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            var me = dependencyObject as EvernoteTagControl;
            me._originalAllTags = new List<ITag>();
            List<ITag> allTags = e.NewValue as List<ITag>;
            if (allTags != null)
            {
                foreach (var tag in allTags)
                {
                    me._originalAllTags.Add(tag);
                }
            }
        }

        // IsEditing, readonly
        public bool IsEditing { get { return (bool)GetValue(IsEditingProperty); } internal set { SetValue(IsEditingPropertyKey, value); } }
        private static readonly DependencyPropertyKey IsEditingPropertyKey = DependencyProperty.RegisterReadOnly("IsEditing", typeof(bool), typeof(EvernoteTagControl), new FrameworkPropertyMetadata(false));
        public static readonly DependencyProperty IsEditingProperty = IsEditingPropertyKey.DependencyProperty;

        public override void OnApplyTemplate()
        {
            Button createBtn = this.GetTemplateChild("PART_CreateTagButton") as Button;
            if (createBtn != null)
                createBtn.Click += createBtn_Click;

            base.OnApplyTemplate();
        }

        /// <summary>
        /// Executed when create new tag button is clicked.
        /// Adds an EvernoteTagItem to the collection and puts it in edit mode.
        /// </summary>
        void createBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ItemsSource != null)
            {
                var used = SelectedTags.ToList();
                AllTags.Clear();
                AllTags.AddRange(_originalAllTags.Where(t => !used.Select(u => u.Id).Contains(t.Id)).ToList());
            }

            if (AllTags.Count == 0) return;
            var newItem = new EvernoteTagItem() { IsEditing = true, DataContext = new Tag()};
            AddTag(newItem);
            this.SelectedItem = newItem;
            this.IsEditing = true;

        }

        /// <summary>
        /// Adds a tag to the collection
        /// </summary>
        internal void AddTag(EvernoteTagItem tag)
        {
            if (this.ItemsSource == null)
                this.ItemsSource = new List<EvernoteTagItem>();

            ((IList)this.ItemsSource).Add(tag); // assume IList for convenience
            SelectedTags.Clear();
            SelectedTags.AddRange(((List<EvernoteTagItem>)this.ItemsSource).Select(d => d.DataContext as ITag));
            this.Items.Refresh();

            if (TagAdded != null)
                TagAdded(this, new EvernoteTagEventArgs(tag));
        }

        /// <summary>
        /// Removes a tag from the collection
        /// </summary>
        internal void RemoveTag(EvernoteTagItem tag, bool cancelEvent = false)
        {
            if (this.ItemsSource != null)
            {
                ((IList)this.ItemsSource).Remove(tag); // assume IList for convenience
                SelectedTags.Clear();
                SelectedTags.AddRange(((List<EvernoteTagItem>)this.ItemsSource).Select(d => d.DataContext as ITag));
                this.Items.Refresh();

                if (TagRemoved != null && !cancelEvent)
                    TagRemoved(this, new EvernoteTagEventArgs(tag));
            }
        }


        /// <summary>
        /// Raises the TagClick event
        /// </summary>
        internal void RaiseTagClick(EvernoteTagItem tag)
        {
            if (this.TagClick != null)
                TagClick(this, new EvernoteTagEventArgs(tag));
        }
    }

    public class EvernoteTagEventArgs : EventArgs
    {
        public EvernoteTagItem Item { get; set; }

        public EvernoteTagEventArgs(EvernoteTagItem item)
        {
            this.Item = item;
        }
    }
}
