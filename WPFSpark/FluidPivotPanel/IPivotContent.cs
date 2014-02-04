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

namespace WPFSpark
{
    /// <summary>
    /// Interface for the PivotContent
    /// </summary>
    public interface IPivotContent
    {
        /// <summary>
        /// Activates/Deactivates the Pivot Content based on the 'isActive' flag.
        /// </summary>
        /// <param name="isActive">Flag to indicate whether the Pivot Content should be Activated or Deactivated.</param>
        void SetActive(bool isActive);
    }
}
