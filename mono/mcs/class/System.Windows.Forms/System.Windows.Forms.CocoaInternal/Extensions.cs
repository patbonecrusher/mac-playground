﻿using System.Runtime.InteropServices;
using System.Windows.Forms.CocoaInternal;

#if MONOMAC
using ObjCRuntime = MonoMac.ObjCRuntime;
using MonoMac.AppKit;
using MonoMac.CoreGraphics;
using MonoMac.Foundation;
using System.Drawing;
#elif XAMARINMAC
using System;
using AppKit;
using Foundation;
#endif

namespace System.Windows.Forms.Mac
{
	public static class Extensions
	{
		static DateTime reference = new DateTime(2001, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

		public static NSDate ToNSDate(this DateTime datetime)
		{
			return NSDate.FromTimeIntervalSinceReferenceDate((datetime.ToUniversalTime() - reference).TotalSeconds);
		}

		public static DateTime ToDateTime(this NSDate date)
		{
			return reference.AddSeconds(date.SecondsSinceReferenceDate).ToLocalTime();
		}

		public static bool IsMouse(this NSEvent e)
		{
			switch (e.Type)
			{
				case NSEventType.LeftMouseDown:
				case NSEventType.RightMouseDown:
				case NSEventType.OtherMouseDown:
				case NSEventType.LeftMouseUp:
				case NSEventType.RightMouseUp:
				case NSEventType.OtherMouseUp:
				case NSEventType.LeftMouseDragged:
				case NSEventType.RightMouseDragged:
				case NSEventType.OtherMouseDragged:
				case NSEventType.ScrollWheel:
				case NSEventType.MouseMoved:
					return true;
			}
			return false;
		}

		public static bool IsMouseDown(this NSEvent e)
		{
			switch (e.Type)
			{
				case NSEventType.LeftMouseDown:
				case NSEventType.RightMouseDown:
				case NSEventType.OtherMouseDown:
					return true;
				default:
					return false;
			}
		}

		public static NSEvent RetargetMouseEvent(this NSEvent e, NSView target)
		{
			var p = target.Window.ConvertScreenToBase(e.Window.ConvertBaseToScreen(e.LocationInWindow));
			return NSEvent.MouseEvent(e.Type, p, e.ModifierFlags, e.Timestamp, target.Window.WindowNumber, null, 0, e.ClickCount, e.Pressure);
		}

		public static void DispatchMouseEvent(this NSView view, NSEvent e)
		{
			switch (e.Type)
			{
				case NSEventType.LeftMouseDown: view.MouseDown(e); break;
				case NSEventType.RightMouseDown: view.RightMouseDown(e); break;
				case NSEventType.OtherMouseDown: view.OtherMouseDown(e); break;
				case NSEventType.LeftMouseUp: view.MouseUp(e); break;
				case NSEventType.RightMouseUp: view.RightMouseUp(e); break;
				case NSEventType.OtherMouseUp: view.OtherMouseUp(e); break;
				case NSEventType.LeftMouseDragged: view.MouseDragged(e); break;
				case NSEventType.RightMouseDragged: view.RightMouseDragged(e); break;
				case NSEventType.OtherMouseDragged: view.OtherMouseDragged(e); break;
				case NSEventType.ScrollWheel: view.ScrollWheel(e); break;
				case NSEventType.BeginGesture: view.BeginGestureWithEvent(e); break;
				case NSEventType.EndGesture: view.EndGestureWithEvent(e); break;
				case NSEventType.MouseMoved: view.MouseMoved(e); break;
			}			
		}

		public static NSObject ToNSObject(this IntPtr handle)
		{
			return ObjCRuntime.Runtime.GetNSObject(handle);
		}

		internal static T ToNSObject<T>(this IntPtr handle) where T : NSObject
		{
			return (T)ObjCRuntime.Runtime.GetNSObject(handle);
		}

		internal static T AsNSObject<T>(this IntPtr handle) where T : NSObject
		{
			return ObjCRuntime.Runtime.GetNSObject(handle) as T;
		}

		internal static NSView ToNSView(this IntPtr handle)
		{
			return (NSView)ObjCRuntime.Runtime.GetNSObject(handle);
		}

		internal static NSView AsNSView(this IntPtr handle)
		{
			return ObjCRuntime.Runtime.GetNSObject(handle) as NSView;
		}

		internal static MonoView AsMonoView(this IntPtr handle)
		{
			return ObjCRuntime.Runtime.GetNSObject(handle) as MonoView;
		}

		public static NSWindow[] OrderedWindows(this NSApplication self)
		{
			var selector = new ObjCRuntime.Selector("orderedWindows");
			var ptr = LibObjc.IntPtr_objc_msgSend(self.Handle, selector.Handle);
			var array = NSArray.ArrayFromHandle<NSWindow>(ptr);
			return array;
		}

		public static NSDragOperation ToDragOperation(this DragDropEffects e)
		{
			var o = NSDragOperation.None;
			if ((e & DragDropEffects.Copy) != 0)
				o |= NSDragOperation.Copy;
			if ((e & DragDropEffects.Link) != 0)
				o |= NSDragOperation.Link;
			if ((e & DragDropEffects.Move) != 0)
				o |= NSDragOperation.Move;
			return o;
		}

		public static DragDropEffects ToDragDropEffects(this NSDragOperation o)
		{
			var e = DragDropEffects.None;
			if ((o & NSDragOperation.Copy) != 0)
				e |= DragDropEffects.Copy;
			if ((o & NSDragOperation.Link) != 0)
				e |= DragDropEffects.Link;
			if ((o & NSDragOperation.Move) != 0)
				e |= DragDropEffects.Move;
			return e;
		}

		public static Keys ToKeys(this NSEventModifierMask modifiers)
		{
			Keys keys = Keys.None;
			if ((NSEventModifierMask.ShiftKeyMask & modifiers) != 0) { keys |= Keys.Shift; }
			if ((NSEventModifierMask.CommandKeyMask & modifiers) != 0) { keys |= Keys.Cmd; }
			if ((NSEventModifierMask.AlternateKeyMask & modifiers) != 0) { keys |= Keys.Alt; }
			if ((NSEventModifierMask.ControlKeyMask & modifiers) != 0) { keys |= Keys.Control; }
			return keys;
		}

		public static int GetUnicodeStringLength(this byte[] self, int max = -1)
		{
			max = max < 0 ? self.Length : Math.Min(max, self.Length);
			for (int n = 0; n < max; n += 2)
				if (self[n] == 0 && self[1 + n] == 0)
					return n;
			return max;
		}

		unsafe internal static void CopyTo(this Runtime.InteropServices.ComTypes.IStream input, System.IO.Stream output, int bufferSize = 32768)
		{
			byte[] buffer = new byte[bufferSize];
			while (true)
			{
				ulong read;
				input.Read(buffer, bufferSize, (IntPtr)(&read));
				if (read == 0)
					return;
				output.Write(buffer, 0, (int)read);
			}
		}

		internal const string FoundationDll = "/System/Library/Frameworks/Foundation.framework/Foundation";

		[DllImport(FoundationDll)]
		public static extern IntPtr NSStringFromClass(IntPtr handle);

		[DllImport(FoundationDll)]
		public static extern IntPtr NSStringFromProtocol(IntPtr handle);

		[DllImport(FoundationDll)]
		public static extern IntPtr NSStringFromSelector(IntPtr handle);

		//NSClassFromString
		//NSSelectorFromString
		//NSProtocolFromString

#if MONOMAC

		public static CGSize SizeThatFits(this NSControl self, CGSize proposedSize)
		{
			var selector = new ObjCRuntime.Selector("sizeThatFits:");
			var size = ObjCRuntime.Messaging.CGSize_objc_msgSend_CGSize(self.Handle, selector.Handle, proposedSize);
			return size;
		}

		public static NSPasteboardWriting AsPasteboardWriting(this NSObject self)
		{
			return new NSPasteboardWriting(self.Handle);
		}

		public static NSPasteboardWriting AsPasteboardWriting(this String self)
		{
			return new NSPasteboardWriting(((NSString)self).Handle);
		}

		// provider must implement NSPasteboardItemDataProvider
		public static void SetDataProviderForTypes(this NSPasteboardItem item, NSObject provider, string[] types)
		{
			var sel = new ObjCRuntime.Selector("setDataProvider:forTypes:");
			var array = NSArray.FromStrings(types);
			var ok = LibObjc.bool_objc_msgSend_IntPtr_IntPtr(item.Handle, sel.Handle, provider.Handle, array.Handle);
		}

        public static T GetItem<T>(this NSArray array, uint index) where T : NSObject
        {
            return (T)ObjCRuntime.Runtime.GetNSObject(array.ValueAt(index));
        }

		public static void WriteObject(this NSPasteboard pboard, NSObject pasteboardWriting)
		{
			// NOTE: pasteboardWriting must conform to NSPasteboardWriting protocol
			var selector = new ObjCRuntime.Selector("writeObjects:");
			var array = NSArray.FromNSObjects(pasteboardWriting);
			ObjCRuntime.Messaging.void_objc_msgSend_IntPtr(pboard.Handle, selector.Handle, array.Handle);
		}

		public static NSData GetData(this NSAttributedString astr, NSRange range, NSDictionary options, out NSError error)
		{
			var selector = new ObjCRuntime.Selector("dataFromRange:documentAttributes:error:");
			var e = new NSError();
			var h = ObjCRuntime.Messaging.IntPtr_objc_msgSend_NSRange_IntPtr_IntPtr_int(astr.Handle, selector.Handle, range, options.Handle, e.Handle, 0);
			error = null;
			return h.AsNSObject<NSData>();
		}


#elif XAMARINMAC

		public static void WriteObject(this NSPasteboard pboard, INSPasteboardWriting pasteboardWriting)
		{
			pboard.WriteObjects(new INSPasteboardWriting[] { pasteboardWriting });
		}

		public static INSPasteboardWriting AsPasteboardWriting(this NSObject self)
		{
			return (INSPasteboardWriting)self;
		}

		public static INSPasteboardWriting AsPasteboardWriting(this String self)
		{
			return (INSPasteboardWriting)(NSString)self;
		}
#endif
	}
}
