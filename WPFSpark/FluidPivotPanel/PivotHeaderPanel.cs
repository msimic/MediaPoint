#region File Header

// -------------------------------------------------------------------------------
// 
// This file is part of the WPFSpark project: http://wpfspark.codeplex.com/
//
// Author: Ratish Philip
// 
// WPFSpark v1.1
//
// -------------------------------------------------------------------------------

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace WPFSpark
{
    /// <summary>
    /// Panel which contains all the headers
    /// </summary>
    public class PivotHeaderPanel : Canvas
    {
        #region Constants

        private const int ADD_FADE_IN_DURATION = 250;
        private const int UPDATE_FADE_IN_DURATION = 50;
        private const int TRANSITION_DURATION = 300;

        #endregion

        #region Events

        public event EventHandler HeaderSelected;

        #endregion

        #region Fields

        Storyboard addFadeInSB;
        Storyboard updateFadeInSB;
        List<UIElement> headerCollection = null;
        Queue<Object[]> animationQueue = null;
        bool isAnimationInProgress = false;
        object syncObject = new object();
        //CubicEase easingFn = null;

        #endregion

        #region Construction / Initialization

        /// <summary>
        /// Ctor
        /// </summary>
        public PivotHeaderPanel()
        {
            // Define the storyboards
            DoubleAnimation addFadeInAnim = new DoubleAnimation(0.0, 1.0, new Duration(TimeSpan.FromMilliseconds(ADD_FADE_IN_DURATION)));
            Storyboard.SetTargetProperty(addFadeInAnim, new PropertyPath(UIElement.OpacityProperty));
            addFadeInSB = new Storyboard();
            addFadeInSB.Children.Add(addFadeInAnim);

            DoubleAnimation updateFadeInAnim = new DoubleAnimation(0.0, 1.0, new Duration(TimeSpan.FromMilliseconds(UPDATE_FADE_IN_DURATION)));
            Storyboard.SetTargetProperty(updateFadeInAnim, new PropertyPath(UIElement.OpacityProperty));
            updateFadeInSB = new Storyboard();
            updateFadeInSB.Children.Add(updateFadeInAnim);

            updateFadeInSB.Completed += new EventHandler(OnAnimationCompleted);

            headerCollection = new List<UIElement>();

        }

        #endregion

        #region APIs

        /// <summary>
        /// Adds a child to the HeaderPanel
        /// </summary>
        /// <param name="child">Child to be added</param>
        public void AddChild(UIElement child)
        {
            if (child == null)
                return;

            lock (syncObject)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    child.Opacity = 0;
                    // Get the Desired size of the child
                    child.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));

                    // Check if the child needs to be added at the end or inserted in between
                    if ((Children.Count == 0) || (Children[Children.Count - 1] == headerCollection.Last()))
                    {
                        child.RenderTransform = CreateTransform(child);
                        Children.Add(child);
                        headerCollection.Add(child);

                        addFadeInSB.Begin((FrameworkElement)child);
                    }
                    else
                    {
                        var lastChild = Children[Children.Count - 1];
                        Children.Add(child);
                        int index = headerCollection.IndexOf(lastChild) + 1;
                        // Insert the new child after the last child in the header collection
                        if (index >= 1)
                        {
                            double newLocationX = ((TranslateTransform)(((TransformGroup)headerCollection[index].RenderTransform).Children[0])).X;
                            headerCollection.Insert(index, child);
                            child.RenderTransform = CreateTransform(new Point(newLocationX, 0.0));

                            InsertChild(child, index + 1);
                        }
                    }

                    // Subscribe to the HeaderSelected event and set Active property to false
                    IPivotHeader headerItem = child as IPivotHeader;

                    if (headerItem != null)
                    {
                        headerItem.HeaderSelected += new EventHandler(OnHeaderSelected);
                    }
                }));
            }
        }

        /// <summary>
        /// Checks if the given UIElement is already added to the Children collection.
        /// </summary>
        /// <param name="child">UIElement</param>
        /// <returns>true/false</returns>
        public bool Contains(UIElement child)
        {
            return Children.Contains(child);
        }

        /// <summary>
        /// Cycles 'count' elements to the left
        /// </summary>
        /// <param name="count">Number of elements to move</param>
        public void MoveForward(int count)
        {
            if ((isAnimationInProgress) || (count <= 0) || (count >= headerCollection.Count))
                return;
            else
                isAnimationInProgress = true;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                // Create the animation queue so that the items removed from the beginning 
                // are added in the end in a sequential manner.
                animationQueue = new Queue<Object[]>();

                lock (animationQueue)
                {
                    for (int i = 0; i < count; i++)
                    {
                        animationQueue.Enqueue(new object[] { headerCollection[i], true });
                    }
                }

                // Get the total width of the first "count" children
                double distanceToMove = ((TranslateTransform)(((TransformGroup)headerCollection[count].RenderTransform).Children[0])).X;

                // Calculate the new location of each child and create appropriate transition
                foreach (UIElement child in headerCollection)
                {
                    double oldTranslationX = ((TranslateTransform)(((TransformGroup)child.RenderTransform).Children[0])).X;
                    double newTranslationX = oldTranslationX - distanceToMove;
                    Storyboard transition = CreateTransition(child, new Point(newTranslationX, 0.0), TimeSpan.FromMilliseconds(TRANSITION_DURATION));
                    // Process the animation queue once the last child's transition is completed
                    if (child == headerCollection.Last())
                    {
                        transition.Completed += (s, e) =>
                        {
                            ProcessAnimationQueue();
                        };
                    }
                    transition.Begin();
                }
            }));
        }

        /// <summary>
        /// Cycles 'count' elements to the right
        /// </summary>
        /// <param name="count">Number of elements to move</param>
        public void MoveBack(int count)
        {
            if (isAnimationInProgress)
                return;
            else
                isAnimationInProgress = true;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                if ((count <= 0) || (count >= headerCollection.Count))
                    return;

                // Create the animation queue so that the items removed from the end 
                // are added at the beginning in a sequential manner.
                animationQueue = new Queue<Object[]>();

                lock (animationQueue)
                {
                    for (int i = headerCollection.Count - 1; i >= headerCollection.Count - count; i--)
                    {
                        animationQueue.Enqueue(new object[] { headerCollection[i], false });
                    }
                }

                // Get the total width of the last "count" number of children
                double distanceToMove = ((TranslateTransform)(((TransformGroup)headerCollection[headerCollection.Count - 1].RenderTransform).Children[0])).X -
                                        ((TranslateTransform)(((TransformGroup)headerCollection[headerCollection.Count - count].RenderTransform).Children[0])).X +
                                        headerCollection[headerCollection.Count - 1].DesiredSize.Width;

                // Calculate the new location of each child and create appropriate transition
                foreach (UIElement child in headerCollection)
                {
                    double oldTranslationX = ((TranslateTransform)(((TransformGroup)child.RenderTransform).Children[0])).X;
                    double newTranslationX = oldTranslationX + distanceToMove;
                    Storyboard transition = CreateTransition(child, new Point(newTranslationX, 0.0), TimeSpan.FromMilliseconds(TRANSITION_DURATION));
                    // Process the animation queue once the last child's transition is completed
                    if (child == headerCollection.Last())
                    {
                        transition.Completed += (s, e) =>
                        {
                            ProcessAnimationQueue();
                        };
                    }
                    transition.Begin();
                }
            }));
        }

        /// <summary>
        /// Removes all the children from the header
        /// </summary>
        public void ClearHeader()
        {
            foreach (UIElement item in headerCollection)
            {
                IPivotHeader headerItem = item as IPivotHeader;

                if (headerItem != null)
                {
                    // Unsubscribe
                    headerItem.HeaderSelected -= OnHeaderSelected;
                }
            }

            headerCollection.Clear();
            Children.Clear();
        }

        /// <summary>
        /// Resets the location of the header items so that the 
        /// first child that was added is moved to the beginning.
        /// </summary>
        internal void Reset()
        {
            if (Children.Count > 0)
            {
                OnHeaderSelected(Children[0], null);
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Inserts the child at the specified index.
        /// </summary>
        /// <param name="child">Child to be inserted</param>
        /// <param name="index">Index at where insertion should be performed</param>
        private void InsertChild(UIElement child, int index)
        {
            double maxH = 0;
            // Move all the children after the 'index' to the right to accommodate the new child
            for (int i = index; i < headerCollection.Count; i++)
            {
                double oldTranslationX = ((TranslateTransform)(((TransformGroup)headerCollection[i].RenderTransform).Children[0])).X;
                double newTranslationX = oldTranslationX + child.DesiredSize.Width;
                headerCollection[i].RenderTransform = CreateTransform(new Point(newTranslationX, 0.0));
                if (maxH < headerCollection[i].DesiredSize.Height)
                {
                    maxH = headerCollection[i].DesiredSize.Height;
                }
            }

            addFadeInSB.Begin((FrameworkElement)child);
        }

        /// <summary>
        /// Appends the child at the beginning or the end based on the isDirectionForward flag
        /// </summary>
        /// <param name="child">Child to be appended</param>
        /// <param name="isDirectionForward">Flag to indicate whether the items has to be added at the end or at the beginning</param>
        private void AppendChild(UIElement child, bool isDirectionForward)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                child.Opacity = 0;
                child.RenderTransform = CreateTransform(child, isDirectionForward);
                headerCollection.Remove(child);
                if (isDirectionForward)
                    headerCollection.Add(child);
                else
                    headerCollection.Insert(0, child);

                double maxH = 0;
                for (int i = 0; i < headerCollection.Count; i++)
                {
                    if (maxH < headerCollection[i].DesiredSize.Height)
                    {
                        maxH = headerCollection[i].DesiredSize.Height;
                    }
                }
                updateFadeInSB.Begin((FrameworkElement)child);
            }));
        }

        /// <summary>
        /// Handles the completed event of each animation in the Animation Queue
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">EventArgs</param>
        private void OnAnimationCompleted(object sender, EventArgs e)
        {
            lock (animationQueue)
            {
                if (animationQueue.Count > 0)
                    animationQueue.Dequeue();
            }

            ProcessAnimationQueue();
        }

        /// <summary>
        /// Process the animation for the next element in the Animation Queue
        /// </summary>
        private void ProcessAnimationQueue()
        {
            lock (animationQueue)
            {
                if (animationQueue.Count > 0)
                {
                    Object[] next = animationQueue.Peek();
                    UIElement child = (UIElement)next[0];
                    bool isDirectionForward = (bool)next[1];
                    AppendChild(child, isDirectionForward);
                }
                else
                {
                    isAnimationInProgress = false;
                }
            }
        }

        /// <summary>
        /// Gets the position available before the first child
        /// </summary>
        /// <returns>Distance on the X-axis</returns>
        private double GetFirstChildPosition()
        {
            double transX = 0.0;

            // Get the first child in the headerCollection
            UIElement firstChild = headerCollection.FirstOrDefault();
            if (firstChild != null)
                transX = ((TranslateTransform)(((TransformGroup)firstChild.RenderTransform).Children[0])).X;

            return transX;
        }

        /// <summary>
        /// Gets the position available after the last child
        /// </summary>
        /// <returns>Distance on the X-axis</returns>
        private double GetNextAvailablePosition()
        {
            double transX = 0.0;
            // Get the last child in the headerCollection 
            UIElement lastChild = headerCollection.LastOrDefault();
            // Add the X-Location of the child + its Desired width to get the next child's position
            if (lastChild != null)
                transX = ((TranslateTransform)(((TransformGroup)lastChild.RenderTransform).Children[0])).X + lastChild.DesiredSize.Width;

            return transX;
        }

        /// <summary>
        /// Creates a translation transform for the child so that it can be placed
        /// at the beginning or the end.
        /// </summary>
        /// <param name="child">Item to be translated</param>
        /// <param name="isDirectionForward">Flag to indicate whether the items has to be added at the end or at the beginning</param>
        /// <returns>TransformGroup</returns>
        private TransformGroup CreateTransform(UIElement child, bool isDirectionForward = true)
        {
            if (child == null)
                return null;

            double transX = 0.0;
            if (isDirectionForward)
                transX = GetNextAvailablePosition();
            else
                // All the children have moved forward to make space for the children to be
                // added in the beginning of the header collection. So calculate the 
                // child's location by subtracting its width from the first child's location
                transX = GetFirstChildPosition() - child.DesiredSize.Width;

            TranslateTransform translation = new TranslateTransform();
            translation.X = transX;
            translation.Y = 0.0;

            TransformGroup transform = new TransformGroup();
            transform.Children.Add(translation);

            return transform;
        }

        /// <summary>
        /// Creates a translation transform
        /// </summary>
        /// <param name="translation">Translation value</param>
        /// <returns>TransformGroup</returns>
        private TransformGroup CreateTransform(Point translation)
        {
            TranslateTransform translateTransform = new TranslateTransform();
            translateTransform.X = translation.X;
            translateTransform.Y = translation.Y;

            TransformGroup transform = new TransformGroup();
            transform.Children.Add(translateTransform);

            return transform;
        }

        /// <summary>
        /// Creates the animation for translating the element
        /// to a new location
        /// </summary>
        /// <param name="element">Item to be translated</param>
        /// <param name="translation">Translation value</param>
        /// <param name="period">Translation duration</param>
        /// <param name="easing">Easing function</param>
        /// <returns>Storyboard</returns>
        private Storyboard CreateTransition(UIElement element, Point translation, TimeSpan period)
        {
            Duration duration = new Duration(period);

            // Animate X
            DoubleAnimation translateAnimationX = new DoubleAnimation();
            translateAnimationX.To = translation.X;
            translateAnimationX.Duration = duration;
            //if (easing != null)
            //    translateAnimationX.EasingFunction = easing;

            Storyboard.SetTarget(translateAnimationX, element);
            Storyboard.SetTargetProperty(translateAnimationX,
                new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(TranslateTransform.X)"));

            Storyboard sb = new Storyboard();
            sb.Children.Add(translateAnimationX);

            return sb;
        }

        #endregion

        #region EventHandlers

        /// <summary>
        /// Handles the HeaderSelected event
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">EventArgs</param>
        void OnHeaderSelected(object sender, EventArgs e)
        {
            if ((isAnimationInProgress) || (headerCollection == null) || (headerCollection.Count == 0))
                return;

            UIElement child = sender as UIElement;
            if (child != null)
            {
                // Check if the header selected is not the first header
                int index = headerCollection.IndexOf(child);
                if (index > 0)
                {
                    // Move the selected header to the left most position
                    MoveForward(index);
                    // Raise the HeaderSelected event
                    if (HeaderSelected != null)
                        HeaderSelected(child, new EventArgs());
                }
            }
        }

        #endregion        
    }
}
