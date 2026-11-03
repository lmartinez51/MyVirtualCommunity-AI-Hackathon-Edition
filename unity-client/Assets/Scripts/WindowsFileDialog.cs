using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace MyVirtualCommunity.Client.Util
{
    /// <summary>
    /// Cross-platform file picker utility providing native desktop file dialogs
    /// for Windows Standalone builds via comdlg32.dll, and falling back to
    /// EditorUtility.OpenFilePanel when running inside the Unity Editor.
    /// </summary>
    public static class WindowsFileDialog
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct OpenFileName
        {
            public int structSize;
            public IntPtr dlgOwner;
            public IntPtr instance;
            public string filter;
            public string customFilter;
            public int maxCustFilter;
            public int filterIndex;
            public string file;
            public int maxFile;
            public string fileTitle;
            public int maxFileTitle;
            public string initialDir;
            public string title;
            public int flags;
            public short fileOffset;
            public short fileExtension;
            public string defExt;
            public IntPtr custData;
            public IntPtr hook;
            public string templateName;
            public IntPtr reservedPtr;
            public int reservedInt;
            public int flagsEx;
        }

        [DllImport("Comdlg32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Auto)]
        public static extern bool GetOpenFileName([In, Out] ref OpenFileName ofn);

        private const int OFN_EXPLORER = 0x00080000;
        private const int OFN_FILEMUSTEXIST = 0x00001000;
        private const int OFN_PATHMUSTEXIST = 0x00000800;
        private const int OFN_NOCHANGEDIR = 0x00000008;

        /// <summary>
        /// Prompts the user to select an authentic photograph (PNG/JPEG) from the desktop.
        /// Returns the absolute path of the selected file, or null if cancelled.
        /// </summary>
        public static string OpenImageFileDialog(string dialogTitle = "Select Image Evidence / Seleccionar Fotografía")
        {
#if UNITY_EDITOR
            string path = UnityEditor.EditorUtility.OpenFilePanel(dialogTitle, "", "png,jpg,jpeg");
            return string.IsNullOrEmpty(path) ? null : path;
#else
            OpenFileName ofn = new OpenFileName();
            ofn.structSize = Marshal.SizeOf(ofn);
            ofn.filter = "Image Files (*.png;*.jpg;*.jpeg)\0*.png;*.jpg;*.jpeg\0All Files (*.*)\0*.*\0";
            ofn.file = new string(new char[1024]);
            ofn.maxFile = ofn.file.Length;
            ofn.fileTitle = new string(new char[1024]);
            ofn.maxFileTitle = ofn.fileTitle.Length;
            ofn.initialDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            ofn.title = dialogTitle;
            ofn.flags = OFN_EXPLORER | OFN_FILEMUSTEXIST | OFN_PATHMUSTEXIST | OFN_NOCHANGEDIR;

            if (GetOpenFileName(ref ofn))
            {
                return ofn.file;
            }
            return null;
#endif
        }
    }
}

