﻿using System.Windows.Forms.CocoaInternal;
using System.Windows.Forms.Mac;

#if XAMARINMAC
using AppKit;
using Foundation;
using CoreGraphics;
#elif MONOMAC
using MonoMac.AppKit;
using MonoMac.CoreGraphics;
using MonoMac.Foundation;
using MonoMac.ObjCRuntime;
using ObjCRuntime = MonoMac.ObjCRuntime;
#endif

namespace System.Windows.Forms
{
	internal partial class XplatUICocoa
	{
		internal const string SwfDragPasteboardType = "mono.dragging.pboard";
		internal const string PublicUtf8PlainText = "public.utf8-plain-text";
		internal const string NSStringPboardType = "NSStringPboardType";

		internal static object DraggedData = null; // TODO: Use pboard. Currently there is a problem with adopting 2 ObjC protocols by a single class.
		internal static DragDropEffects DraggingAllowedEffects = DragDropEffects.None;
		internal static DragDropEffects DraggingEffects = DragDropEffects.None;
		internal static NSDraggingSession DraggingSession = null;

		MonoDraggingSource draggingSource = new MonoDraggingSource();
		internal NSEvent lastMouseEvent = null;

		internal override void SetAllowDrop(IntPtr handle, bool value)
		{
			if (ObjCRuntime.Runtime.GetNSObject(handle) is MonoView view)
			{
				if (value)
					view.RegisterForDraggedTypes(new string[] { SwfDragPasteboardType, NSPasteboard.NSStringType });
				else
					view.UnregisterDraggedTypes();
			}
		}

		internal override DragDropEffects StartDrag(IntPtr handle, object data, DragDropEffects allowedEffects)
		{
			if (ObjCRuntime.Runtime.GetNSObject(handle) is MonoView view)
			{
				if (Grab.Hwnd != IntPtr.Zero)
					UngrabWindow(Grab.Hwnd);

				var items = CreateDraggingItems(view, DraggedData = data);
				if (items != null && items.Length != 0)
				{
					DraggingSession = view.BeginDraggingSession(items, lastMouseEvent, draggingSource);
					DraggingAllowedEffects = allowedEffects;
					DraggingEffects = DragDropEffects.None;
				}
			}

			return allowedEffects;
		}

		internal virtual NSDraggingItem[] CreateDraggingItems(NSView view, object data)
		{
			var maxSize = new CGSize(320, 240);
			var size = ScaleToFit(view.Bounds.Size, maxSize);
			var location = view.ConvertPointFromView(lastMouseEvent.LocationInWindow, null);
			var bounds = new CGRect(location.Move(-4, -4), size);

			NSDraggingItem item = null;
			switch (data)
			{
				case String s:
					item = new NSDraggingItem(s.AsPasteboardWriting());
					item.SetDraggingFrame(bounds, TakeSnapshot(view));
					break;
				default:
					item = new NSDraggingItem(SwfDragPasteboardType.AsPasteboardWriting());
					item.SetDraggingFrame(bounds, TakeSnapshot(view));
					DraggedData = data;
					break;
			}

			return new NSDraggingItem[] { item };
		}

		internal static NSImage TakeSnapshot(NSView view)
		{
			var b = view.BitmapImageRepForCachingDisplayInRect(view.Bounds);
			view.CacheDisplay(view.Bounds, b);

			var i = new NSImage(view.Bounds.Size);
			i.AddRepresentation(b);

			return i;
		}

		internal static CGSize ScaleToFit(CGSize val, CGSize max)
		{
			var kw = max.Width / val.Width;
			var kh = max.Height / val.Height;

			var k = Math.Min(kw, kh);
			k = Math.Min(k, 1.0f);

			return new CGSize(k * val.Width, k * val.Height);
		}

		internal static NSImage ResizeImage(NSImage image, CGSize size)
		{
			var resized = new NSImage(size);
			resized.LockFocus();
			NSGraphicsContext.CurrentContext.ImageInterpolation = NSImageInterpolation.High;
			image.DrawInRect(new CGRect(0, 0, size.Width, size.Height),new CGRect(CGPoint.Empty, image.Size), NSCompositingOperation.Copy,1.0f);
			resized.UnlockFocus();
	        return resized;			
		}
	}

	internal class MonoDraggingSource : NSDraggingSource
	{
		public override void DraggedImageBeganAt(NSImage image, CGPoint screenPoint)
		{
			//Console.WriteLine("MonoDraggingSource.DraggedImageBeganAt");
		}

		public override void DraggedImageEndedAtOperation(NSImage image, CGPoint screenPoint, NSDragOperation operation)
		{
			//Console.WriteLine($"MonoDraggingSource.DraggedImageEndedAtOperation({screenPoint.X},{screenPoint.Y},{operation}");

			XplatUICocoa.DraggedData = null;
			XplatUICocoa.DraggingSession = null;
			XplatUICocoa.DraggingEffects = MonoView.ToDragDropEffects(operation);
		}

		public override void DraggedImageMovedTo(NSImage image, CGPoint screenPoint)
		{
			// Jde pouzit napr. k vypoctu polohy v textu pro vlozeni
			//Console.WriteLine("DraggedImageMovedTo");
		}

		public override NSDragOperation DraggingSourceOperationMaskForLocal(bool flag)
		{
			//Console.WriteLine("MonoDraggingSource.DraggingSourceOperationMaskForLocal");
			return MonoView.ToDragOperation(XplatUICocoa.DraggingEffects);
		}

		public override string[] NamesOfPromisedFilesDroppedAtDestination(NSUrl dropDestination)
		{
			//Console.WriteLine("MonoDraggingSource.NamesOfPromisedFilesDroppedAtDestination");
			return new string[] { };
		}
	}


}