﻿using System;
using System.Drawing;
using System.Runtime.InteropServices;
using DlibDotNet;
using OpenCvSharp;
using Microsoft.Win32;

namespace CakeLock
{
	public class Program
	{
		private static VideoCapture capture = new VideoCapture(0);
		private static Mat image = new Mat();
		public static void Main(string[] args)
		{
			Console.WriteLine("Starting Recognizer");
			RecognizeFaceAndLock(capture, image);

			SystemEvents.SessionSwitch += new Microsoft.Win32.SessionSwitchEventHandler(SystemEvents_SessionSwitch);
			Console.ReadLine();
		}

		public static void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
		{
			if (e.Reason == SessionSwitchReason.SessionLock)
			{
				Console.WriteLine("PC Locked");
			}
			else if (e.Reason == SessionSwitchReason.SessionUnlock)
			{
				Console.WriteLine("PC Unlocked");
				RecognizeFaceAndLock(capture, image);
			}
		}

		public static void RecognizeFaceAndLock(VideoCapture capture, Mat image)
		{
			Console.WriteLine("Started looking for you.");
			bool active = true;
			var NoFaceCount = 0;
			while (active)
			{
				capture.Read(image);
				if (image.Empty())
					break;

				Bitmap bmp = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(image);

				var faceDetected = FaceDetector(bmp);

				if (faceDetected)
				{
					NoFaceCount = 0;
				}
				else
				{
					NoFaceCount++;
				}

				Console.WriteLine(faceDetected ? "Face detected: " + faceDetected : "Face detected: " + faceDetected + " - " + NoFaceCount);

				if (NoFaceCount >= 25)
				{
					LockWorkStation();
					active = false;
				}
			}
		}

		public static bool FaceDetector(Bitmap bmp)
		{
			var rect = new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height);
			var data = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, bmp.PixelFormat);

			var array = new byte[data.Stride * data.Height];
			Marshal.Copy(data.Scan0, array, 0, array.Length);

			using (var faceDetector = Dlib.GetFrontalFaceDetector())
			{
				var img = Dlib.LoadImageData<BgrPixel>(array, (uint)bmp.Height, (uint)bmp.Width, (uint)data.Stride);

				var faces = faceDetector.Operator(img);

				return faces.Length > 0; 
			}
		}

		[DllImport("user32.dll")]
		public static extern bool LockWorkStation();
	}
}
