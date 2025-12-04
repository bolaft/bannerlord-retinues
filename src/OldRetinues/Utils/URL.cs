using System;
using System.Diagnostics;
using TaleWorlds.Library;

namespace Retinues.Utils
{
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
    //                    URL Utilities                   //
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

    /// <summary>
    /// Utilities for handling URLs.
    /// </summary>
    [SafeClass]
    public static class URL
    {
        /// <summary>
        /// Open a URL in the system default web browser.
        /// </summary>
        public static void OpenInBrowser(string url)
        {
            try
            {
                var psi = new ProcessStartInfo(url)
                {
                    UseShellExecute = true, // required to open URLs with the OS default handler
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(
                    new InformationMessage(
                        L.T("open_browser_fail", "Failed to open browser. URL: {URL}")
                            .SetTextVariable("URL", url)
                            .ToString()
                    )
                );

                Log.Error($"Failed to open URL '{url}': {ex}");
            }
        }
    }
}
