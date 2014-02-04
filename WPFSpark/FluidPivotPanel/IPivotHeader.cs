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

namespace WPFSpark
{
    /// <summary>
    /// Interface for the PivotHeader
    /// </summary>
    public interface IPivotHeader
    {
        /// <summary>
        /// Activates/Deactivates the Pivot Header based on the 'isActive' flag.
        /// </summary>
        /// <param name="isActive">Flag to indicate whether the Pivot Header should be Activated or Deactivated.</param>
        void SetActive(bool isActive);
        /// <summary>
        /// Event fired when the header is selected by the user
        /// </summary>
        event EventHandler HeaderSelected;
    }
}
