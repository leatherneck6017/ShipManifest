﻿using System;
using System.Collections.Generic;
using ConnectedLivingSpace;
using ShipManifest.Process;
using UnityEngine;

namespace ShipManifest.InternalObjects
{
  // ReSharper disable once InconsistentNaming
  internal static class SMHighlighter
  {
    #region Properties

    // WindowTransfer part mouseover vars
    internal static bool IsMouseOver;
    internal static TransferPump.TypePump MouseOverMode = TransferPump.TypePump.SourceToTarget;
    internal static Rect MouseOverRect = new Rect(0, 0, 0, 0);
    internal static Part MouseOverPart;
    internal static List<Part> MouseOverParts;
    internal static Part PrevMouseOverPart;
    internal static List<Part> PrevMouseOverParts;

    #endregion

    #region Methods

    internal static void ClearPartsHighlight(List<Part> parts)
    {
      if (parts == null) return;
      List<Part>.Enumerator list = parts.GetEnumerator();
      while (list.MoveNext())
      {
        ClearPartHighlight(list.Current);          
      }
      list.Dispose();
    }

    /// <summary>
    ///   Remove highlighting on a part.
    /// </summary>
    /// <param name="part">Part to remove highlighting from.</param>
    internal static void ClearPartHighlight(Part part)
    {
      if (part == null) return;
      if (part.HighlightActive) part.SetHighlightDefault();
    }

    /// <summary>
    ///   Removes Highlighting on parts belonging to the selected resource list.
    /// </summary>
    /// <param name="resourceParts"></param>
    internal static void ClearResourceHighlighting(List<Part> resourceParts)
    {
      if (resourceParts == null) return;
      List<Part>.Enumerator list = resourceParts.GetEnumerator();
      while (list.MoveNext())
      {
        ClearPartHighlight(list.Current);
      }
      list.Dispose();
      if (!SMSettings.EnableCls || !SMSettings.EnableClsHighlighting || !SMAddon.GetClsVessel()) return;
      if (resourceParts.Count > 0 && resourceParts[0] != null)
        SMAddon.ClsAddon.Vessel.Highlight(false);
    }

    internal static void SetPartsHighlight(List<Part> parts, Color color, bool force = false)
    {
      try
      {
        List<Part>.Enumerator list = parts.GetEnumerator();
        while (list.MoveNext())
        {
          if (list.Current == null) continue;
          if (!list.Current.HighlightActive || force)
          SetPartHighlight(list.Current, color);
        }
        list.Dispose();
      }
      catch (Exception ex)
      {
        SMUtils.LogMessage(
          $" in  SetPartsHighlight.  Error:  {ex.Message} \r\n\r\n{ex.StackTrace}", SMUtils.LogType.Error, true);
      }
    }

    internal static void SetPartHighlight(Part part, Color color)
    {
      if (part == null) return;
      try
      {
        if (!part.HighlightActive)
          part.SetHighlight(true, false);
        part.highlightType = Part.HighlightType.AlwaysOn;
        part.SetHighlightColor(color);
      }
      catch (Exception ex)
      {
        SMUtils.LogMessage(
          $" in  SetPartHighlight.  Error:  {ex.Message} \r\n\r\n{ex.StackTrace}", SMUtils.LogType.Error, true);
      }
    }

    internal static void MouseOverHighlight()
    {
      // Supports Transfer Window vessel/part and Control window part Highlighting....
      RevertMouseOverHighlight();
      if (MouseOverPart == null && MouseOverParts != null)
      {
        MouseOverHighlight(MouseOverParts);
        PrevMouseOverParts = MouseOverParts;
        MouseOverParts = null;
      }
      else if (MouseOverPart != null)
      {
        MouseOverHighlight(MouseOverPart);
        PrevMouseOverPart = MouseOverPart;
        MouseOverPart = null;
      }
    }

    internal static void MouseOverHighlight(Part part)
    {
      string step = "begin";
      try
      {
        step = "inside box - Part Selection?";
        SetPartHighlight(part, SMSettings.Colors[SMSettings.MouseOverColor]);
        EdgeHighight(part, true);
      }
      catch (Exception ex)
      {
        SMUtils.LogMessage($" in SMHighlighter.MouseOverHighlight at step {step}.  Error:  {ex}",
          SMUtils.LogType.Error, true);
      }
    }

    internal static void MouseOverHighlight(List<Part> parts)
    {
        SetPartsHighlight(parts, SMSettings.Colors[SMSettings.MouseOverColor]);
        EdgeHighight(parts, true);
    }

    internal static void RevertMouseOverHighlight()
    {
      // Lets get the right color...

      if (PrevMouseOverParts != null)
      {
        // Only vessel Transfers have multiple parts highlighted  so clear is the default.
        // now lets set the part colors.
        ClearPartsHighlight(PrevMouseOverParts);;
        EdgeHighight(PrevMouseOverParts, false);

        // now set selected part colors...
        if (!SMSettings.OnlySourceTarget && SMAddon.SmVessel.SelectedResourcesParts != null)
          SetPartsHighlight(SMAddon.SmVessel.SelectedResourcesParts, SMSettings.Colors[SMSettings.ResourcePartColor], true);
        if (SMAddon.SmVessel.SelectedPartsSource != null)
          SetPartsHighlight(SMAddon.SmVessel.SelectedPartsSource, SMSettings.Colors[SMSettings.SourcePartColor],true);
        if (SMAddon.SmVessel.SelectedPartsTarget != null)
          SetPartsHighlight(SMAddon.SmVessel.SelectedPartsTarget, SMSettings.Colors[SMSettings.TargetPartColor], true);

        if (!IsMouseOver) PrevMouseOverParts = null;
      }

      if (PrevMouseOverPart != null)
      {
        string strColor = GetPartHighlightColor();
        Color partColor = SMSettings.Colors[strColor];

        // Now lets set the part color
        if (partColor == Color.clear)
        {
          ClearPartHighlight(PrevMouseOverPart);
          EdgeHighight(PrevMouseOverPart, false);
          if(!IsMouseOver) PrevMouseOverPart = null;
          return;
        }
        SetPartHighlight(PrevMouseOverPart, partColor);
        EdgeHighight(PrevMouseOverPart, true, strColor);
        if (!IsMouseOver) PrevMouseOverPart = null;
      }
    }

    private static string GetPartHighlightColor()
    {
      string strColor = "clear";
      // Here, we need to also account for a part selected in the Control window.
      // so we can have a part revert to nothing...
      //if (PrevMouseOverPart == MouseOverPart || PrevMouseOverPart == null) return strColor;
      if (SMAddon.SmVessel.SelectedPartsSource.Contains(PrevMouseOverPart))
        strColor = SMSettings.SourcePartColor;
      else if (SMAddon.SmVessel.SelectedPartsTarget.Contains(PrevMouseOverPart))
        strColor = SMSettings.TargetPartColor;
      else if (SMAddon.SmVessel.SelectedResourcesParts.Contains(PrevMouseOverPart) && !SMSettings.OnlySourceTarget)
      {
        strColor = SMConditions.IsClsHighlightingEnabled() ? "green" : "yellow";
      }
      return strColor;
    }

    internal static void EdgeHighight(Part part, bool enable, string color = null)
    {
      if (!SMSettings.EnableEdgeHighlighting) return;
      if (enable)
      {
        if (string.IsNullOrEmpty(color))
          color = SMSettings.MouseOverColor;
        part.highlighter.SeeThroughOn();
        part.highlighter.ConstantOnImmediate(SMSettings.Colors[color]);
      }
      else
      {
        part.highlighter.SeeThroughOff();
        part.highlighter.ConstantOffImmediate();
      }
    }

    internal static void EdgeHighight(List<Part> parts, bool enable, string color = null)
    {
      if (!SMSettings.EnableEdgeHighlighting) return;
      List<Part>.Enumerator list = parts.GetEnumerator();
      while (list.MoveNext())
      {
        if (list.Current == null) continue;
        if (enable)
        {
          if (string.IsNullOrEmpty(color))
            color = SMSettings.MouseOverColor;
          list.Current.highlighter.SeeThroughOn();
          list.Current.highlighter.ConstantOnImmediate(SMSettings.Colors[color]);
        }
        else
        {
          list.Current.highlighter.SeeThroughOff();
          list.Current.highlighter.ConstantOffImmediate();
        }
      }
      list.Dispose();
    }

    internal static void HighlightClsVessel(bool enabled, bool force = false)
    {
      try
      {
        if (SMAddon.ClsAddon.Vessel == null)
          SMAddon.UpdateClsSpaces();
        if (SMAddon.ClsAddon.Vessel == null) return;
        List<ICLSSpace>.Enumerator spaces = SMAddon.ClsAddon.Vessel.Spaces.GetEnumerator();
        while (spaces.MoveNext())
        {
          if (spaces.Current == null) continue;
          List<ICLSPart>.Enumerator parts = spaces.Current.Parts.GetEnumerator();
          while (parts.MoveNext())
          {
            parts.Current?.Highlight(enabled, force);
          }
          parts.Dispose();
        }
        spaces.Dispose();
      }
      catch (Exception ex)
      {
        if (!SMAddon.FrameErrTripped)
        {
          SMUtils.LogMessage(
            $" in HighlightCLSVessel (repeating error).  Error:  {ex.Message} \r\n\r\n{ex.StackTrace}", SMUtils.LogType.Error, true);
          SMAddon.FrameErrTripped = true;
        }
      }
    }

    // This method is expensive.  Refactor to consume less CPU.
    internal static void Update_Highlighter()
    {
      string step = "";
      try
      {
        // Do we even want to highlight?
        if (!SMSettings.EnableHighlighting) return;
        step = "Showhipmanifest = true";
        if (!SMConditions.CanShowShipManifest()) return;
        //step = "Clear old highlighting";
        // Clear Highlighting on everything, start fresh
        EdgeHighight(SMAddon.SmVessel.Vessel.parts, false);
        ClearPartsHighlight(SMAddon.SmVessel.Vessel.parts);

        if (SMAddon.SmVessel.SelectedResources != null && SMAddon.SmVessel.SelectedResources.Count > 0)
        {
          // If Crew and cls, perform cls Highlighting
          if (SMConditions.IsClsHighlightingEnabled())
          {
            step = "Highlight CLS vessel";
            HighlightClsVessel(true, true);

            // Turn off the source and target cls highlighting.  We are going to replace it.
            if (SMAddon.SmVessel.ClsPartSource != null)
              SMAddon.SmVessel.ClsPartSource.Highlight(false, true);
            if (SMAddon.SmVessel.ClsPartTarget != null)
              SMAddon.SmVessel.ClsPartTarget.Highlight(false, true);
          }

          // Default is yellow
          step = "Set non selected resource part color";
          Color partColor = SMSettings.Colors[SMSettings.ResourcePartColor];

          // match color used by CLS if active
          if (SMAddon.SmVessel.SelectedResources.Contains(SMConditions.ResourceType.Crew.ToString()) &&
              SMSettings.EnableCls)
            partColor = Color.green;

          step = "Set Resource Part Colors";
          if (!SMSettings.OnlySourceTarget)
          {
            SetPartsHighlight(SMAddon.SmVessel.SelectedResourcesParts, partColor);
          }

          step = "Set Selected Part Colors";
          SetPartsHighlight(SMAddon.SmVessel.SelectedPartsSource, SMSettings.Colors[SMSettings.SourcePartColor], true);
          if (SMAddon.SmVessel.SelectedResources.Contains(SMConditions.ResourceType.Crew.ToString()) &&
              SMSettings.EnableCls)
            SetPartsHighlight(SMAddon.SmVessel.SelectedPartsTarget,
              SMSettings.Colors[SMSettings.TargetPartCrewColor], true);
          else
            SetPartsHighlight(SMAddon.SmVessel.SelectedPartsTarget, SMSettings.Colors[SMSettings.TargetPartColor], true);
        }
      }
      catch (Exception ex)
      {
        if (!SMAddon.FrameErrTripped)
        {
          SMUtils.LogMessage($" in SMHighlighter.UpdateHighlighting (repeating error).  Error in step:  {step}.  Error:  {ex.Message}\n\n{ex.StackTrace}",
            SMUtils.LogType.Error, true);
          SMAddon.FrameErrTripped = true;
        }
      }
    }

    internal static Color GetHighlightColor(Part part, out string colorstring)
    {
      colorstring = "clear";
      if (SMPart.IsSource(part)) colorstring = SMSettings.SourcePartColor;
      else if (SMPart.IsTarget(part)) colorstring = SMPart.IsCrew(part)
        ? SMPart.IsClsTarget(part) ? SMSettings.TargetPartCrewColor : SMSettings.TargetPartColor
        : SMSettings.TargetPartColor;
      else if (SMPart.IsSelected(part)) colorstring = SMSettings.ResourcePartColor;
      return SMSettings.Colors[colorstring];
    }

    #endregion
  }
}