#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Indicators;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    public class ZUPHarmonicPatterns : Indicator
    {
        #region Variables
        private Series<double> highs4H;
        private Series<double> lows4H;
        private Series<DateTime> times4H;
        private Series<int> bars4H;
        
        private List<PivotPoint> pivotHighs;
        private List<PivotPoint> pivotLows;
        private List<HarmonicPattern> detectedPatterns;
        
        private bool isCalculatingOn4H = false;
        
        // Price point values for different instruments
        private Dictionary<string, double> instrumentPricePoints = new Dictionary<string, double>()
        {
            {"GC", 1.0},      // Gold
            {"6E", 0.0001},   // Euro
            {"6B", 0.0001},   // British Pound
            {"6A", 0.0001},   // Australian Dollar
            {"6J", 0.0001},   // Japanese Yen
            {"6C", 0.0001},   // Canadian Dollar
            {"6S", 0.0001},   // Swiss Franc
            {"NQ", 0.25},     // Nasdaq
            {"ES", 0.25},     // S&P 500
            {"YM", 1.0},      // Dow Jones
            {"RTY", 0.1},     // Russell 2000
            {"CL", 0.01},     // Crude Oil
            {"NG", 0.001},    // Natural Gas
            {"ZB", 0.03125},  // 30-Year Treasury Bond
            {"ZN", 0.015625}, // 10-Year Treasury Note
            {"ZF", 0.0078125}, // 5-Year Treasury Note
            {"ZC", 0.25},     // Corn
            {"ZS", 0.25},     // Soybeans
            {"ZW", 0.25},     // Wheat
            {"HG", 0.0005},   // Copper
            {"SI", 0.005},    // Silver
            {"PA", 0.05},     // Palladium
            {"PL", 0.1}       // Platinum
        };
        #endregion
        
        #region Data Structures
        public class PivotPoint
        {
            public double Price { get; set; }
            public int BarIndex { get; set; }
            public DateTime Time { get; set; }
            public bool IsHigh { get; set; }
            
            public PivotPoint(double price, int barIndex, DateTime time, bool isHigh)
            {
                Price = price;
                BarIndex = barIndex;
                Time = time;
                IsHigh = isHigh;
            }
        }
        
        public class HarmonicPattern
        {
            public PivotPoint X { get; set; }
            public PivotPoint A { get; set; }
            public PivotPoint B { get; set; }
            public PivotPoint C { get; set; }
            public PivotPoint D { get; set; }
            public string PatternType { get; set; }
            public bool IsBullish { get; set; }
            public bool IsDrawn { get; set; }
            
            public HarmonicPattern(PivotPoint x, PivotPoint a, PivotPoint b, PivotPoint c, PivotPoint d, string patternType, bool isBullish)
            {
                X = x;
                A = a;
                B = b;
                C = c;
                D = d;
                PatternType = patternType;
                IsBullish = isBullish;
                IsDrawn = false;
            }
        }
        #endregion
        
        public override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"ZUP-style harmonic pattern indicator that detects patterns on 4H timeframe";
                Name = "ZUPHarmonicPatterns";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                DisplayInDataBox = true;
                DrawOnPricePanel = true;
                DrawHorizontalGridLines = true;
                DrawVerticalGridLines = true;
                PaintPriceMarkers = true;
                ScaleJustification = NinjaTrader.Gui.Chart.ScaleJustification.Right;
                IsSuspendedWhileInactive = true;
                
                // Pattern detection parameters
                PivotStrength = 5;
                ShowBullishPatterns = true;
                ShowBearishPatterns = true;
                PatternLineColor = Brushes.Yellow;
                PatternLineWidth = 2;
                RectangleColor = Brushes.Orange;
                LabelColor = Brushes.White;
                EnableAlerts = true;
            }
            else if (State == State.DataLoaded)
            {
                pivotHighs = new List<PivotPoint>();
                pivotLows = new List<PivotPoint>();
                detectedPatterns = new List<HarmonicPattern>();
                
                // Add 4H data series
                AddDataSeries(BarsPeriodType.Minute, 240);
            }
        }
        
        public override void OnBarUpdate()
        {
            if (BarsInProgress == 0) return; // Only process 4H data
            
            if (BarsInProgress == 1) // 4H data series
            {
                isCalculatingOn4H = true;
                CalculatePivots();
                DetectHarmonicPatterns();
                isCalculatingOn4H = false;
            }
        }
        
        private void CalculatePivots()
        {
            if (CurrentBars[1] < PivotStrength * 2) return;
            
            int currentBar = CurrentBars[1];
            double currentHigh = Highs[1][0];
            double currentLow = Lows[1][0];
            DateTime currentTime = Times[1][0];
            
            // Check for pivot high
            bool isPivotHigh = true;
            for (int i = 1; i <= PivotStrength; i++)
            {
                if (Highs[1][i] >= Highs[1][PivotStrength] || Highs[1][PivotStrength - i] >= Highs[1][PivotStrength])
                {
                    isPivotHigh = false;
                    break;
                }
            }
            
            // Check for pivot low
            bool isPivotLow = true;
            for (int i = 1; i <= PivotStrength; i++)
            {
                if (Lows[1][i] <= Lows[1][PivotStrength] || Lows[1][PivotStrength - i] <= Lows[1][PivotStrength])
                {
                    isPivotLow = false;
                    break;
                }
            }
            
            if (isPivotHigh)
            {
                PivotPoint pivot = new PivotPoint(Highs[1][PivotStrength], currentBar - PivotStrength, Times[1][PivotStrength], true);
                pivotHighs.Add(pivot);
                
                // Keep only last 20 pivots for performance
                if (pivotHighs.Count > 20)
                    pivotHighs.RemoveAt(0);
            }
            
            if (isPivotLow)
            {
                PivotPoint pivot = new PivotPoint(Lows[1][PivotStrength], currentBar - PivotStrength, Times[1][PivotStrength], false);
                pivotLows.Add(pivot);
                
                // Keep only last 20 pivots for performance
                if (pivotLows.Count > 20)
                    pivotLows.RemoveAt(0);
            }
        }
        
        private void DetectHarmonicPatterns()
        {
            if (pivotHighs.Count < 3 || pivotLows.Count < 3) return;
            
            // Detect bullish patterns (point D should be at a low pivot)
            if (ShowBullishPatterns)
            {
                foreach (var dPoint in pivotLows.Where(p => !detectedPatterns.Any(dp => dp.D.BarIndex == p.BarIndex)))
                {
                    DetectBullishPatterns(dPoint);
                }
            }
            
            // Detect bearish patterns (point D should be at a high pivot)
            if (ShowBearishPatterns)
            {
                foreach (var dPoint in pivotHighs.Where(p => !detectedPatterns.Any(dp => dp.D.BarIndex == p.BarIndex)))
                {
                    DetectBearishPatterns(dPoint);
                }
            }
        }
        
        private void DetectBullishPatterns(PivotPoint dPoint)
        {
            var validCPoints = pivotHighs.Where(p => p.BarIndex < dPoint.BarIndex).OrderByDescending(p => p.BarIndex).Take(5);
            
            foreach (var cPoint in validCPoints)
            {
                var validBPoints = pivotLows.Where(p => p.BarIndex < cPoint.BarIndex).OrderByDescending(p => p.BarIndex).Take(5);
                
                foreach (var bPoint in validBPoints)
                {
                    var validAPoints = pivotHighs.Where(p => p.BarIndex < bPoint.BarIndex).OrderByDescending(p => p.BarIndex).Take(5);
                    
                    foreach (var aPoint in validAPoints)
                    {
                        var validXPoints = pivotLows.Where(p => p.BarIndex < aPoint.BarIndex).OrderByDescending(p => p.BarIndex).Take(5);
                        
                        foreach (var xPoint in validXPoints)
                        {
                            string patternType = IdentifyPattern(xPoint, aPoint, bPoint, cPoint, dPoint, true);
                            if (!string.IsNullOrEmpty(patternType))
                            {
                                var pattern = new HarmonicPattern(xPoint, aPoint, bPoint, cPoint, dPoint, patternType, true);
                                
                                // Check if this pattern already exists
                                if (!PatternAlreadyExists(pattern))
                                {
                                    detectedPatterns.Add(pattern);
                                    DrawPattern(pattern);
                                    TriggerAlert(pattern);
                                }
                                return; // Only detect one pattern per D point
                            }
                        }
                    }
                }
            }
        }
        
        private void DetectBearishPatterns(PivotPoint dPoint)
        {
            var validCPoints = pivotLows.Where(p => p.BarIndex < dPoint.BarIndex).OrderByDescending(p => p.BarIndex).Take(5);
            
            foreach (var cPoint in validCPoints)
            {
                var validBPoints = pivotHighs.Where(p => p.BarIndex < cPoint.BarIndex).OrderByDescending(p => p.BarIndex).Take(5);
                
                                 foreach (var bPoint in validBPoints)
                 {
                     var validAPoints = pivotLows.Where(p => p.BarIndex < bPoint.BarIndex).OrderByDescending(p => p.BarIndex).Take(5);
                    
                    foreach (var aPoint in validAPoints)
                    {
                        var validXPoints = pivotHighs.Where(p => p.BarIndex < aPoint.BarIndex).OrderByDescending(p => p.BarIndex).Take(5);
                        
                        foreach (var xPoint in validXPoints)
                        {
                            string patternType = IdentifyPattern(xPoint, aPoint, bPoint, cPoint, dPoint, false);
                            if (!string.IsNullOrEmpty(patternType))
                            {
                                var pattern = new HarmonicPattern(xPoint, aPoint, bPoint, cPoint, dPoint, patternType, false);
                                
                                // Check if this pattern already exists
                                if (!PatternAlreadyExists(pattern))
                                {
                                    detectedPatterns.Add(pattern);
                                    DrawPattern(pattern);
                                    TriggerAlert(pattern);
                                }
                                return; // Only detect one pattern per D point
                            }
                        }
                    }
                }
            }
        }
        
        private string IdentifyPattern(PivotPoint x, PivotPoint a, PivotPoint b, PivotPoint c, PivotPoint d, bool isBullish)
        {
            double ab_xa = Math.Abs(b.Price - a.Price) / Math.Abs(a.Price - x.Price);
            double bc_ab = Math.Abs(c.Price - b.Price) / Math.Abs(b.Price - a.Price);
            double cd_bc = Math.Abs(d.Price - c.Price) / Math.Abs(c.Price - b.Price);
            double ad_xa = Math.Abs(d.Price - a.Price) / Math.Abs(a.Price - x.Price);
            
            // Gartley Pattern
            if (IsWithinTolerance(ab_xa, 0.618, 0.05) && 
                IsWithinTolerance(bc_ab, 0.382, 0.05) && 
                IsWithinTolerance(cd_bc, 1.272, 0.05) && 
                IsWithinTolerance(ad_xa, 0.786, 0.05))
            {
                return "Gartley";
            }
            
            // Bat Pattern
            if (IsWithinTolerance(ab_xa, 0.382, 0.05) && 
                IsWithinTolerance(bc_ab, 0.382, 0.05) && 
                IsWithinTolerance(cd_bc, 1.618, 0.05) && 
                IsWithinTolerance(ad_xa, 0.886, 0.05))
            {
                return "Bat";
            }
            
            // Crab Pattern
            if (IsWithinTolerance(ab_xa, 0.382, 0.05) && 
                IsWithinTolerance(bc_ab, 0.382, 0.05) && 
                IsWithinTolerance(cd_bc, 2.24, 0.1) && 
                IsWithinTolerance(ad_xa, 1.618, 0.05))
            {
                return "Crab";
            }
            
            // Butterfly Pattern
            if (IsWithinTolerance(ab_xa, 0.786, 0.05) && 
                IsWithinTolerance(bc_ab, 0.382, 0.05) && 
                IsWithinTolerance(cd_bc, 1.618, 0.05) && 
                IsWithinTolerance(ad_xa, 1.27, 0.05))
            {
                return "Butterfly";
            }
            
            return null;
        }
        
        private bool IsWithinTolerance(double value, double target, double tolerance)
        {
            return Math.Abs(value - target) <= tolerance;
        }
        
        private bool PatternAlreadyExists(HarmonicPattern newPattern)
        {
            return detectedPatterns.Any(p => 
                p.D.BarIndex == newPattern.D.BarIndex && 
                p.PatternType == newPattern.PatternType && 
                p.IsBullish == newPattern.IsBullish);
        }
        
        private void DrawPattern(HarmonicPattern pattern)
        {
            if (pattern.IsDrawn) return;
            
            string patternId = $"{pattern.PatternType}_{pattern.D.BarIndex}";
            
            // Convert 4H bar indices to primary timeframe
            int xBar = GetPrimaryTimeframeBar(pattern.X.Time);
            int aBar = GetPrimaryTimeframeBar(pattern.A.Time);
            int bBar = GetPrimaryTimeframeBar(pattern.B.Time);
            int cBar = GetPrimaryTimeframeBar(pattern.C.Time);
            int dBar = GetPrimaryTimeframeBar(pattern.D.Time);
            
            // Draw pattern lines
            Draw.Line(this, $"{patternId}_XA", false, xBar, pattern.X.Price, aBar, pattern.A.Price, PatternLineColor, DashStyleHelper.Solid, PatternLineWidth);
            Draw.Line(this, $"{patternId}_AB", false, aBar, pattern.A.Price, bBar, pattern.B.Price, PatternLineColor, DashStyleHelper.Solid, PatternLineWidth);
            Draw.Line(this, $"{patternId}_BC", false, bBar, pattern.B.Price, cBar, pattern.C.Price, PatternLineColor, DashStyleHelper.Solid, PatternLineWidth);
            Draw.Line(this, $"{patternId}_CD", false, cBar, pattern.C.Price, dBar, pattern.D.Price, PatternLineColor, DashStyleHelper.Solid, PatternLineWidth);
            Draw.Line(this, $"{patternId}_XB", false, xBar, pattern.X.Price, bBar, pattern.B.Price, PatternLineColor, DashStyleHelper.Dash, PatternLineWidth);
            Draw.Line(this, $"{patternId}_AC", false, aBar, pattern.A.Price, cBar, pattern.C.Price, PatternLineColor, DashStyleHelper.Dash, PatternLineWidth);
            Draw.Line(this, $"{patternId}_BD", false, bBar, pattern.B.Price, dBar, pattern.D.Price, PatternLineColor, DashStyleHelper.Dash, PatternLineWidth);
            
            // Calculate rectangle dimensions
            double pricePoint = GetPricePointForInstrument();
            double rectTop = pattern.D.Price + (10 * pricePoint);
            double rectBottom = pattern.D.Price - (10 * pricePoint);
            
            // Calculate end time (3 calendar days from point D)
            DateTime endTime = pattern.D.Time.AddDays(3);
            int endBar = GetPrimaryTimeframeBar(endTime);
            
            // Draw rectangle at point D
            Draw.Rectangle(this, $"{patternId}_Rect", false, dBar, rectTop, endBar, rectBottom, RectangleColor, RectangleColor, 0);
            
            // Draw label
            string direction = pattern.IsBullish ? "Bullish" : "Bearish";
            string labelText = $"{direction} {pattern.PatternType}";
            Draw.Text(this, $"{patternId}_Label", false, labelText, dBar, rectTop + (5 * pricePoint), 0, LabelColor, new SimpleFont("Arial", 10), TextAlignment.Center, Brushes.Transparent, Brushes.Transparent, 0);
            
            pattern.IsDrawn = true;
        }
        
        private double GetPricePointForInstrument()
        {
            string instrumentName = Instrument.FullName;
            
            // Try to find exact match first
            if (instrumentPricePoints.ContainsKey(instrumentName))
                return instrumentPricePoints[instrumentName];
            
            // Try to find partial match
            foreach (var kvp in instrumentPricePoints)
            {
                if (instrumentName.Contains(kvp.Key))
                    return kvp.Value;
            }
            
            // Default to tick size if no match found
            return Instrument.MasterInstrument.TickSize;
        }
        
        private int GetPrimaryTimeframeBar(DateTime time)
        {
            // Find the closest bar in primary timeframe to the given time
            for (int i = 0; i < CurrentBar; i++)
            {
                if (Times[0][i] <= time)
                    return CurrentBar - i;
            }
            return CurrentBar;
        }
        
        private void TriggerAlert(HarmonicPattern pattern)
        {
            if (!EnableAlerts) return;
            
            string direction = pattern.IsBullish ? "Bullish" : "Bearish";
            string message = $"{direction} {pattern.PatternType} pattern detected at {pattern.D.Price:F2}";
            
            Alert($"{pattern.PatternType}Alert", Priority.High, message, NinjaTrader.Core.Globals.InstallDir + @"\sounds\Alert1.wav", 10, Brushes.Yellow, Brushes.Black);
        }
        
        #region Properties
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name="Pivot Strength", Description="Number of bars to look back and forward for pivot detection", Order=1, GroupName="Parameters")]
        public int PivotStrength { get; set; }
        
        [NinjaScriptProperty]
        [Display(Name="Show Bullish Patterns", Description="Display bullish harmonic patterns", Order=2, GroupName="Parameters")]
        public bool ShowBullishPatterns { get; set; }
        
        [NinjaScriptProperty]
        [Display(Name="Show Bearish Patterns", Description="Display bearish harmonic patterns", Order=3, GroupName="Parameters")]
        public bool ShowBearishPatterns { get; set; }
        
        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name="Pattern Line Color", Description="Color of the pattern lines", Order=4, GroupName="Appearance")]
        public Brush PatternLineColor { get; set; }
        
        [Browsable(false)]
        public string PatternLineColorSerializable
        {
            get { return Serialize.BrushToString(PatternLineColor); }
            set { PatternLineColor = Serialize.StringToBrush(value); }
        }
        
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name="Pattern Line Width", Description="Width of the pattern lines", Order=5, GroupName="Appearance")]
        public int PatternLineWidth { get; set; }
        
        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name="Rectangle Color", Description="Color of the rectangle at point D", Order=6, GroupName="Appearance")]
        public Brush RectangleColor { get; set; }
        
        [Browsable(false)]
        public string RectangleColorSerializable
        {
            get { return Serialize.BrushToString(RectangleColor); }
            set { RectangleColor = Serialize.StringToBrush(value); }
        }
        
        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name="Label Color", Description="Color of the pattern labels", Order=7, GroupName="Appearance")]
        public Brush LabelColor { get; set; }
        
        [Browsable(false)]
        public string LabelColorSerializable
        {
            get { return Serialize.BrushToString(LabelColor); }
            set { LabelColor = Serialize.StringToBrush(value); }
        }
        
        [NinjaScriptProperty]
        [Display(Name="Enable Alerts", Description="Enable pop-up alerts when patterns are detected", Order=8, GroupName="Alerts")]
        public bool EnableAlerts { get; set; }
        #endregion
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
    public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
    {
        private ZUPHarmonicPatterns[] cacheZUPHarmonicPatterns;
        
        public ZUPHarmonicPatterns ZUPHarmonicPatterns(int pivotStrength, bool showBullishPatterns, bool showBearishPatterns, Brush patternLineColor, int patternLineWidth, Brush rectangleColor, Brush labelColor, bool enableAlerts)
        {
            return ZUPHarmonicPatterns(Input, pivotStrength, showBullishPatterns, showBearishPatterns, patternLineColor, patternLineWidth, rectangleColor, labelColor, enableAlerts);
        }
        
        public ZUPHarmonicPatterns ZUPHarmonicPatterns(ISeries<double> input, int pivotStrength, bool showBullishPatterns, bool showBearishPatterns, Brush patternLineColor, int patternLineWidth, Brush rectangleColor, Brush labelColor, bool enableAlerts)
        {
            if (cacheZUPHarmonicPatterns != null)
                for (int idx = 0; idx < cacheZUPHarmonicPatterns.Length; idx++)
                    if (cacheZUPHarmonicPatterns[idx] != null && cacheZUPHarmonicPatterns[idx].PivotStrength == pivotStrength && cacheZUPHarmonicPatterns[idx].ShowBullishPatterns == showBullishPatterns && cacheZUPHarmonicPatterns[idx].ShowBearishPatterns == showBearishPatterns && cacheZUPHarmonicPatterns[idx].PatternLineColor == patternLineColor && cacheZUPHarmonicPatterns[idx].PatternLineWidth == patternLineWidth && cacheZUPHarmonicPatterns[idx].RectangleColor == rectangleColor && cacheZUPHarmonicPatterns[idx].LabelColor == labelColor && cacheZUPHarmonicPatterns[idx].EnableAlerts == enableAlerts && cacheZUPHarmonicPatterns[idx].EqualsInput(input))
                        return cacheZUPHarmonicPatterns[idx];
            return CacheIndicator<ZUPHarmonicPatterns>(new ZUPHarmonicPatterns(){ PivotStrength = pivotStrength, ShowBullishPatterns = showBullishPatterns, ShowBearishPatterns = showBearishPatterns, PatternLineColor = patternLineColor, PatternLineWidth = patternLineWidth, RectangleColor = rectangleColor, LabelColor = labelColor, EnableAlerts = enableAlerts }, input, ref cacheZUPHarmonicPatterns);
        }
    }
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
    public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
    {
        public Indicators.ZUPHarmonicPatterns ZUPHarmonicPatterns(int pivotStrength, bool showBullishPatterns, bool showBearishPatterns, Brush patternLineColor, int patternLineWidth, Brush rectangleColor, Brush labelColor, bool enableAlerts)
        {
            return indicator.ZUPHarmonicPatterns(Input, pivotStrength, showBullishPatterns, showBearishPatterns, patternLineColor, patternLineWidth, rectangleColor, labelColor, enableAlerts);
        }
        
        public Indicators.ZUPHarmonicPatterns ZUPHarmonicPatterns(ISeries<double> input , int pivotStrength, bool showBullishPatterns, bool showBearishPatterns, Brush patternLineColor, int patternLineWidth, Brush rectangleColor, Brush labelColor, bool enableAlerts)
        {
            return indicator.ZUPHarmonicPatterns(input, pivotStrength, showBullishPatterns, showBearishPatterns, patternLineColor, patternLineWidth, rectangleColor, labelColor, enableAlerts);
        }
    }
}

namespace NinjaTrader.NinjaScript.Strategies
{
    public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
    {
        public Indicators.ZUPHarmonicPatterns ZUPHarmonicPatterns(int pivotStrength, bool showBullishPatterns, bool showBearishPatterns, Brush patternLineColor, int patternLineWidth, Brush rectangleColor, Brush labelColor, bool enableAlerts)
        {
            return indicator.ZUPHarmonicPatterns(Input, pivotStrength, showBullishPatterns, showBearishPatterns, patternLineColor, patternLineWidth, rectangleColor, labelColor, enableAlerts);
        }
        
        public Indicators.ZUPHarmonicPatterns ZUPHarmonicPatterns(ISeries<double> input , int pivotStrength, bool showBullishPatterns, bool showBearishPatterns, Brush patternLineColor, int patternLineWidth, Brush rectangleColor, Brush labelColor, bool enableAlerts)
        {
            return indicator.ZUPHarmonicPatterns(input, pivotStrength, showBullishPatterns, showBearishPatterns, patternLineColor, patternLineWidth, rectangleColor, labelColor, enableAlerts);
        }
    }
}

#endregion