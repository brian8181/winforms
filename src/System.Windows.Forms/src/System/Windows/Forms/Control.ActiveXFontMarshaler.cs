﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Drawing;
using System.Runtime.InteropServices;
using Windows.Win32.System.Com;
using Windows.Win32.System.Ole;

namespace System.Windows.Forms;

public partial class Control
{
    /// <summary>
    ///  This is a marshaler object that knows how to marshal IFont to Font and back.
    /// </summary>
    private unsafe class ActiveXFontMarshaler : ICustomMarshaler
    {
        private static ActiveXFontMarshaler? s_instance;

        public void CleanUpManagedData(object obj)
        {
        }

        public void CleanUpNativeData(IntPtr pObj) => Marshal.Release(pObj);

        internal static ICustomMarshaler GetInstance(string cookie)
        {
            return s_instance ??= new ActiveXFontMarshaler();
        }

        public int GetNativeDataSize() => -1; // not a value type, so use -1

        public nint MarshalManagedToNative(object obj)
        {
            Font font = (Font)obj;
            LOGFONTW logFont = LOGFONTW.FromFont(font);

            fixed (char* n = font.Name)
            {
                FONTDESC fontDesc = new()
                {
                    cbSizeofstruct = (uint)sizeof(FONTDESC),
                    lpstrName = n,
                    cySize = (CY)font.SizeInPoints,
                    sWeight = (short)logFont.lfWeight,
                    sCharset = (short)logFont.lfCharSet,
                    fItalic = font.Italic,
                    fUnderline = font.Underline,
                    fStrikethrough = font.Strikeout,
                };

                PInvoke.OleCreateFontIndirect(
                    in fontDesc,
                    in IID.GetRef<IFont>(),
                    out void* lplpvObj).ThrowOnFailure();

                return (nint)lplpvObj;
            }
        }

        public object MarshalNativeToManaged(nint pObj)
        {
            IFont.Interface nativeFont = (IFont.Interface)Marshal.GetObjectForIUnknown(pObj);
            try
            {
                return Font.FromHfont(nativeFont.hFont);
            }
            catch (Exception e) when (!e.IsCriticalException())
            {
                return DefaultFont;
            }
        }
    }
}
