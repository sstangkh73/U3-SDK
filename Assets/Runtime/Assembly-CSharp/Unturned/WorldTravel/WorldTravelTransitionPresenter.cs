////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the U3 SDK: https://github.com/smartlydressedgames/u3-sdk/    //
// Please refer to the included LICENSE.txt for copyright notice and license details. //
////////////////////////////////////////////////////////////////////////////////////////
using System;
using UnityEngine;

namespace SDG.Unturned
{
	/// <summary>
	/// Draws a persistent tunnel over the menu and loading scenes used by world travel. This keeps
	/// the existing one-map-at-a-time memory model while making the hand-off appear continuous.
	/// </summary>
	internal sealed class WorldTravelTransitionPresenter : MonoBehaviour
	{
		private const float SOURCE_FADE_DURATION = 0.8f;
		private const float TARGET_REVEAL_DELAY = 0.65f;
		private const float TARGET_REVEAL_DURATION = 1.15f;

		private Texture2D sourceFrame;
		private string destinationName;
		private bool isVisible;
		private bool isRevealing;
		private float shownAt;
		private float revealAt;
		private GUIStyle destinationStyle;
		private GUIStyle statusStyle;

		public void PrepareSourceCapture(string destination)
		{
			destinationName = destination;
			isRevealing = false;
			isVisible = false;
			DestroySourceFrame();
		}

		public void CaptureSourceFrame()
		{
			try
			{
				sourceFrame = ScreenCapture.CaptureScreenshotAsTexture();
			}
			catch (Exception exception)
			{
				UnturnedLog.warn($"Unable to capture source frame for world travel transition: {exception.Message}");
				DestroySourceFrame();
			}
		}

		public void ShowTransition()
		{
			isVisible = true;
			isRevealing = false;
			shownAt = Time.realtimeSinceStartup;
		}

		public void EnsureTransitionVisible(string destination)
		{
			destinationName = destination;
			if (!isVisible)
			{
				// The source fade is not useful if the presenter was recreated between scenes.
				shownAt = Time.realtimeSinceStartup - SOURCE_FADE_DURATION;
				isVisible = true;
			}
			isRevealing = false;
		}

		public void BeginReveal()
		{
			if (!isVisible)
			{
				return;
			}

			isRevealing = true;
			revealAt = Time.realtimeSinceStartup + TARGET_REVEAL_DELAY;
		}

		public void CancelTransition()
		{
			isVisible = false;
			isRevealing = false;
			DestroySourceFrame();
		}

		private void Update()
		{
			if (!isVisible)
			{
				return;
			}

			// MenuStartup normally makes the cursor visible, but it is only an implementation detail
			// during this automatic hand-off and should not appear over the tunnel.
			Cursor.visible = false;

			if (isRevealing && Time.realtimeSinceStartup >= revealAt + TARGET_REVEAL_DURATION)
			{
				isVisible = false;
				isRevealing = false;
				DestroySourceFrame();
			}
		}

		private void OnGUI()
		{
			if (!isVisible)
			{
				return;
			}

			int previousDepth = GUI.depth;
			Color previousColor = GUI.color;
			GUI.depth = -10000;

			float opacity = GetTransitionOpacity();
			DrawTunnel(opacity);

			float elapsed = Time.realtimeSinceStartup - shownAt;
			if (sourceFrame != null && elapsed < SOURCE_FADE_DURATION)
			{
				float sourceOpacity = 1.0f - Mathf.SmoothStep(0.0f, 1.0f, elapsed / SOURCE_FADE_DURATION);
				GUI.color = new Color(1.0f, 1.0f, 1.0f, sourceOpacity * opacity);
				GUI.DrawTexture(new Rect(0.0f, 0.0f, Screen.width, Screen.height), sourceFrame,
					ScaleMode.ScaleAndCrop, false);

				float darkPulse = Mathf.Sin(Mathf.Clamp01(elapsed / SOURCE_FADE_DURATION) * Mathf.PI);
				DrawSolid(new Rect(0.0f, 0.0f, Screen.width, Screen.height),
					new Color(0.0f, 0.0f, 0.0f, darkPulse * 0.85f * opacity));
			}

			DrawLabels(opacity);
			GUI.color = previousColor;
			GUI.depth = previousDepth;
		}

		private float GetTransitionOpacity()
		{
			if (!isRevealing || Time.realtimeSinceStartup < revealAt)
			{
				return 1.0f;
			}

			float progress = (Time.realtimeSinceStartup - revealAt) / TARGET_REVEAL_DURATION;
			return 1.0f - Mathf.SmoothStep(0.0f, 1.0f, Mathf.Clamp01(progress));
		}

		private void DrawTunnel(float opacity)
		{
			float width = Screen.width;
			float height = Screen.height;
			float centerX = width * 0.5f;
			float centerY = height * 0.46f;

			DrawSolid(new Rect(0.0f, 0.0f, width, height), new Color(0.006f, 0.012f, 0.02f, opacity));
			DrawSolid(new Rect(width * 0.18f, height * 0.12f, width * 0.64f, height * 0.68f),
				new Color(0.015f, 0.055f, 0.075f, 0.7f * opacity));

			float time = Time.realtimeSinceStartup;
			for (int index = 0; index < 10; ++index)
			{
				float progress = Mathf.Repeat((time * 0.22f) + (index / 10.0f), 1.0f);
				float perspective = progress * progress;
				float halfWidth = Mathf.Lerp(width * 0.035f, width * 0.59f, perspective);
				float halfHeight = Mathf.Lerp(height * 0.025f, height * 0.58f, perspective);
				float thickness = Mathf.Lerp(1.0f, Mathf.Max(8.0f, height * 0.018f), perspective);
				float lightAlpha = Mathf.Lerp(0.12f, 0.62f, progress) * opacity;
				Color light = new Color(0.2f, 0.82f, 0.95f, lightAlpha);

				DrawSolid(new Rect(centerX - halfWidth, centerY - halfHeight, halfWidth * 2.0f, thickness), light);
				DrawSolid(new Rect(centerX - halfWidth, centerY + halfHeight - thickness, halfWidth * 2.0f, thickness), light);
				DrawSolid(new Rect(centerX - halfWidth, centerY - halfHeight, thickness, halfHeight * 2.0f), light);
				DrawSolid(new Rect(centerX + halfWidth - thickness, centerY - halfHeight, thickness, halfHeight * 2.0f), light);
			}

			float pulse = 0.55f + (Mathf.Sin(time * 3.0f) * 0.15f);
			DrawSolid(new Rect(centerX - width * 0.012f, centerY - height * 0.009f,
				width * 0.024f, height * 0.018f), new Color(0.65f, 0.95f, 1.0f, pulse * opacity));
			DrawSolid(new Rect(0.0f, height * 0.78f, width, height * 0.22f),
				new Color(0.003f, 0.008f, 0.012f, 0.92f * opacity));
		}

		private void DrawLabels(float opacity)
		{
			EnsureStyles();
			string displayDestination = string.IsNullOrWhiteSpace(destinationName)
				? "NEXT REGION"
				: destinationName.ToUpperInvariant();
			int dotCount = 1 + (Mathf.FloorToInt(Time.realtimeSinceStartup * 2.2f) % 3);
			string dots = new string('.', dotCount);

			GUI.color = new Color(1.0f, 1.0f, 1.0f, opacity);
			GUI.Label(new Rect(0.0f, Screen.height * 0.825f, Screen.width, Screen.height * 0.07f),
				$"CROSSING TO {displayDestination}", destinationStyle);
			GUI.Label(new Rect(0.0f, Screen.height * 0.89f, Screen.width, Screen.height * 0.05f),
				"Travelling through the connection" + dots, statusStyle);
		}

		private void EnsureStyles()
		{
			if (destinationStyle == null)
			{
				destinationStyle = new GUIStyle(GUI.skin.label)
				{
					alignment = TextAnchor.MiddleCenter,
					fontStyle = FontStyle.Bold
				};
				destinationStyle.normal.textColor = new Color(0.78f, 0.95f, 1.0f);
			}

			if (statusStyle == null)
			{
				statusStyle = new GUIStyle(GUI.skin.label)
				{
					alignment = TextAnchor.UpperCenter
				};
				statusStyle.normal.textColor = new Color(0.55f, 0.72f, 0.78f);
			}

			destinationStyle.fontSize = Mathf.Clamp(Screen.height / 28, 20, 48);
			statusStyle.fontSize = Mathf.Clamp(Screen.height / 50, 14, 26);
		}

		private static void DrawSolid(Rect rectangle, Color color)
		{
			GUI.color = color;
			GUI.DrawTexture(rectangle, Texture2D.whiteTexture, ScaleMode.StretchToFill, false);
		}

		private void DestroySourceFrame()
		{
			if (sourceFrame != null)
			{
				Destroy(sourceFrame);
				sourceFrame = null;
			}
		}

		private void OnDestroy()
		{
			DestroySourceFrame();
		}
	}
}
