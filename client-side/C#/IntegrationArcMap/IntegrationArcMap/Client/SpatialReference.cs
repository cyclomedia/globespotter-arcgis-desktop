﻿/*
 * Integration in ArcMap for Cycloramas
 * Copyright (c) 2014, CycloMedia, All rights reserved.
 * 
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3.0 of the License, or (at your option) any later version.
 * 
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library.
 */

using System;
using System.Xml.Serialization;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geometry;
using IntegrationArcMap.Utilities;

namespace IntegrationArcMap.Client
{
  public class SpatialReference
  {
    #region members

    // =========================================================================
    // Members
    // =========================================================================
    private ISpatialReference _spatialReference;

    #endregion

    #region properties

    // =========================================================================
    // Properties
    // =========================================================================
    // ReSharper disable InconsistentNaming
    public string Name { get; set; }

    public string SRSName { get; set; }

    public string Units { get; set; }

    public Bounds NativeBounds { get; set; }

    public string ESRICompatibleName { get; set; }

    public string CompatibleSRSNames { get; set; }
    // ReSharper restore InconsistentNaming

    [XmlIgnore]
    public bool CanMeasuring
    {
      get { return ((Units == "m") || (Units == "ft")); }
    }

    [XmlIgnore]
    public ISpatialReference SpatialRef
    {
      get
      {
        if (_spatialReference == null)
        {
          if (string.IsNullOrEmpty(SRSName))
          {
            _spatialReference = null;
          }
          else
          {
            int srs;
            string strsrs = SRSName.Replace("EPSG:", string.Empty);

            if (int.TryParse(strsrs, out srs))
            {
              ISpatialReferenceFactory3 spatialRefFactory = new SpatialReferenceEnvironmentClass();

              try
              {
                _spatialReference = spatialRefFactory.CreateProjectedCoordinateSystem(srs);
              }
              catch (ArgumentException)
              {
                try
                {
                  _spatialReference = spatialRefFactory.CreateGeographicCoordinateSystem(srs);
                }
                catch (ArgumentException)
                {
                  if (string.IsNullOrEmpty(CompatibleSRSNames))
                  {
                    _spatialReference = null;
                  }
                  else
                  {
                    strsrs = CompatibleSRSNames.Replace("EPSG:", string.Empty);

                    if (int.TryParse(strsrs, out srs))
                    {
                      try
                      {
                        _spatialReference = spatialRefFactory.CreateProjectedCoordinateSystem(srs);
                      }
                      catch (ArgumentException)
                      {
                        try
                        {
                          _spatialReference = spatialRefFactory.CreateGeographicCoordinateSystem(srs);
                        }
                        catch (ArgumentException)
                        {
                          _spatialReference = null;
                        }
                      }
                    }
                    else
                    {
                      _spatialReference = null;
                    }
                  }
                }
              }
            }
            else
            {
              _spatialReference = null;
            }
          }
        }

        if (_spatialReference != null)
        {
          IActiveView activeView = ArcUtils.ActiveView;
          IEnvelope envelope = activeView.Extent;
          ISpatialReference spatEnv = envelope.SpatialReference;

          Config config = Config.Instance;
          string defaultRecordingSrs = config.DefaultRecordingSrs;
          int spatEnvFactoryCode = 0;

          if ((spatEnv != null) && ((string.IsNullOrEmpty(defaultRecordingSrs)) || (!int.TryParse(defaultRecordingSrs, out spatEnvFactoryCode))))
          {
            spatEnvFactoryCode = spatEnv.FactoryCode;
          }

          if ((spatEnv != null) && (spatEnvFactoryCode != _spatialReference.FactoryCode))
          {
            IEnvelope copyEnvelope = envelope.Envelope;
            copyEnvelope.Project(_spatialReference);

            if (copyEnvelope.IsEmpty)
            {
              _spatialReference = null;
            }
            else
            {
              if (NativeBounds != null)
              {
                double xMin = NativeBounds.MinX;
                double yMin = NativeBounds.MinY;
                double xMax = NativeBounds.MaxX;
                double yMax = NativeBounds.MaxY;

                if ((copyEnvelope.XMin < xMin) || (copyEnvelope.XMax > xMax) || (copyEnvelope.YMin < yMin) ||
                    (copyEnvelope.YMax > yMax))
                {
                  _spatialReference = null;
                }
              }
            }
          }
        }

        return _spatialReference;
      }
    }

    [XmlIgnore]
    public bool KnownInArcMap
    {
      get { return SpatialRef != null; }
    }

    #endregion

    #region functions (public)

    // =========================================================================
    // Functions (Public)
    // =========================================================================
    public override string ToString()
    {
      return string.Format("{0} ({1})", (string.IsNullOrEmpty(ESRICompatibleName) ? Name : ESRICompatibleName), SRSName);
    }

    #endregion
  }
}
