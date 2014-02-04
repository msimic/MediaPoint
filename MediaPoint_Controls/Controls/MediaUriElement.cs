using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using MediaPoint.Common.DirectShow.MediaPlayers;
using System.Collections.ObjectModel;
using MediaPoint.VM.ViewInterfaces;

namespace MediaPoint.Controls
{
    /// <summary>
    /// The MediaUriElement is a WPF control that plays media of a given
    /// Uri. The Uri can be a file path or a Url to media.  The MediaUriElement
    /// inherits from the MediaSeekingElement, so where available, seeking is
    /// also supported.
    /// </summary>
    public class MediaUriElement : MediaSeekingElement
    {
        /// <summary>
        /// The current MediaUriPlayer
        /// </summary>
        public MediaUriPlayer MediaUriPlayer
        {
            get
            {
                return MediaPlayerBase as MediaUriPlayer;
            }
        }

        #region Loop

        public static readonly DependencyProperty LoopProperty =
            DependencyProperty.Register("Loop", typeof(bool), typeof(MediaUriElement),
                new FrameworkPropertyMetadata(false,
                    new PropertyChangedCallback(OnLoopChanged)));

        /// <summary>
        /// Gets or sets whether the media should return to the begining
        /// once the end has reached
        /// </summary>
        public bool Loop
        {
            get { return (bool)GetValue(LoopProperty); }
            set { SetValue(LoopProperty, value); }
        }

        private static void OnLoopChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((MediaUriElement)d).OnLoopChanged(e);
        }

        protected virtual void OnLoopChanged(DependencyPropertyChangedEventArgs e)
        {
            if (HasInitialized)
                PlayerSetLoop();
        }

        private void PlayerSetLoop()
        {
			bool designTime = DesignerProperties.GetIsInDesignMode(new DependencyObject());
			if (designTime) return;

            var loop = Loop;
            MediaPlayerBase.Dispatcher.BeginInvoke((Action)delegate
            {
                MediaUriPlayer.Loop = loop;
            });
        }
        #endregion

        public override void EndInit()
        {
            PlayerSetVideoRenderer();
            PlayerSetAudioRenderer();
            PlayerSetLoop();
            PlayerSetSource();
            base.EndInit();
        }

        /// <summary>
        /// The Play method is overrided so we can
        /// set the source to the media
        /// </summary>
        public override void Play()
        {
            EnsurePlayerThread();
            base.Play();
        }

        /// <summary>
        /// The Pause method is overrided so we can
        /// set the source to the media
        /// </summary>
        public override void Pause()
        {
            EnsurePlayerThread();

            base.Pause();
        }

        /// <summary>
        /// Gets the instance of the media player to initialize
        /// our base classes with
        /// </summary>
        protected override MediaPlayerBase OnRequestMediaPlayer()
        {
            var player = new MediaUriPlayer();
			AudioRenderers = new ObservableCollection<string>(MediaUriPlayer.AudioRenderers);
            return player;
        }

	}
}
