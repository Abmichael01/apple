# ZUP Harmonic Patterns Indicator for NinjaTrader 8

## Overview

This ZUP-style harmonic pattern indicator detects and displays harmonic patterns (Gartley, Bat, Crab, and Butterfly) on the 4-hour timeframe while allowing visualization on any chart timeframe, particularly the 60-minute chart as specified.

## Key Features

### ✅ Core Functionality
- **4H Timeframe Detection**: Patterns are detected exclusively on the 4-hour timeframe regardless of the chart timeframe
- **Multi-Timeframe Display**: Load on 60-minute chart to view 4H harmonic patterns
- **Pattern Types**: Detects Gartley, Bat, Crab, and Butterfly patterns
- **Correct Pivot Logic**: 
  - Bullish patterns: Point D must be at a low pivot (for potential long setups)
  - Bearish patterns: Point D must be at a high pivot (for potential short setups)
- **No Duplicate Signals**: Each pattern appears only once per valid formation

### 📊 Visual Elements
- **Unfilled Pattern Outlines**: Clean line drawings without color fill
- **Rectangle at Point D**: Fixed-size rectangle extending 3 calendar days into the future
- **Instrument-Specific Sizing**: Rectangle height automatically adjusts based on instrument type
- **Pattern Labels**: Shows pattern type and direction (e.g., "Bullish Gartley")
- **Alerts**: Pop-up notifications when patterns are detected

### 🎯 Rectangle Sizing (Per Instrument)
The rectangle at point D extends 10 price points above and below the pivot:

| Instrument | Price Point Value | Example Height |
|------------|-------------------|----------------|
| GC (Gold) | 1.0 | 20 points |
| 6E (Euro) | 0.0001 | 0.002 |
| 6B (British Pound) | 0.0001 | 0.002 |
| ES (S&P 500) | 0.25 | 5.0 points |
| NQ (Nasdaq) | 0.25 | 5.0 points |
| CL (Crude Oil) | 0.01 | 0.20 |

*Note: If your instrument isn't listed, the indicator defaults to using the instrument's tick size.*

## Installation Instructions

### Step 1: Copy the Indicator File
1. Save the `ZUPHarmonicPatterns.cs` file to your NinjaTrader 8 indicators folder:
   ```
   Documents\NinjaTrader 8\bin\Custom\Indicators\
   ```

### Step 2: Compile the Indicator
1. Open NinjaTrader 8
2. Go to **Tools** → **NinjaScript Editor** (or press F11)
3. In the NinjaScript Editor, click **Compile** (or press F5)
4. Check for any compilation errors in the output window
5. If successful, close the NinjaScript Editor

### Step 3: Add to Chart
1. Open a 60-minute chart of your desired instrument
2. Right-click on the chart → **Indicators**
3. Find **ZUPHarmonicPatterns** in the list
4. Double-click to add it to your chart
5. Configure settings as needed (see Configuration section)

## Configuration Options

### Parameters Group
- **Pivot Strength**: Number of bars to look back/forward for pivot detection (Default: 5)
- **Show Bullish Patterns**: Enable/disable bullish pattern detection (Default: True)
- **Show Bearish Patterns**: Enable/disable bearish pattern detection (Default: True)

### Appearance Group
- **Pattern Line Color**: Color of the harmonic pattern lines (Default: Yellow)
- **Pattern Line Width**: Thickness of pattern lines (Default: 2)
- **Rectangle Color**: Color of the rectangle at point D (Default: Orange)
- **Label Color**: Color of pattern labels (Default: White)

### Alerts Group
- **Enable Alerts**: Turn on/off pop-up alerts (Default: True)

## Pattern Recognition Ratios

The indicator uses these Fibonacci ratios for pattern identification:

### Gartley Pattern
- AB/XA: 0.618 (±5% tolerance)
- BC/AB: 0.382 (±5% tolerance)
- CD/BC: 1.272 (±5% tolerance)
- AD/XA: 0.786 (±5% tolerance)

### Bat Pattern
- AB/XA: 0.382 (±5% tolerance)
- BC/AB: 0.382 (±5% tolerance)
- CD/BC: 1.618 (±5% tolerance)
- AD/XA: 0.886 (±5% tolerance)

### Crab Pattern
- AB/XA: 0.382 (±5% tolerance)
- BC/AB: 0.382 (±5% tolerance)
- CD/BC: 2.24 (±10% tolerance)
- AD/XA: 1.618 (±5% tolerance)

### Butterfly Pattern
- AB/XA: 0.786 (±5% tolerance)
- BC/AB: 0.382 (±5% tolerance)
- CD/BC: 1.618 (±5% tolerance)
- AD/XA: 1.27 (±5% tolerance)

## Usage Tips

### Best Practices
1. **Load on 60M Chart**: While the indicator detects patterns on 4H, load it on your 60-minute chart for optimal visualization
2. **Pattern Validation**: Wait for pattern completion at point D before taking action
3. **Risk Management**: The 3-day rectangle provides a time-based reference for pattern validity
4. **Multiple Timeframes**: Consider using additional timeframe analysis for confirmation

### Troubleshooting
- **No Patterns Showing**: Ensure you have sufficient historical data (at least 100+ 4H bars)
- **Performance Issues**: The indicator limits to 20 stored pivots for optimal performance
- **Rectangle Size Issues**: Check that your instrument is included in the price point dictionary

### Alert Settings
When patterns are detected, you'll receive:
- Pop-up notification with pattern type and direction
- Audio alert (default NinjaTrader sound)
- Pattern location (price level)

## Supported Instruments

The indicator includes optimized price point values for:

**Forex**: 6E, 6B, 6A, 6J, 6C, 6S  
**Indices**: ES, NQ, YM, RTY  
**Commodities**: GC, SI, CL, NG, HG, PA, PL  
**Bonds**: ZB, ZN, ZF  
**Agriculture**: ZC, ZS, ZW  

## Technical Notes

- **Data Series**: Automatically adds 4H data series (240-minute bars)
- **Memory Management**: Maintains only the last 20 pivots for performance
- **Pattern Persistence**: Drawn patterns remain visible when scrolling
- **Duplicate Prevention**: Sophisticated logic prevents multiple signals for the same pattern

## Version Information
- **Version**: 1.0
- **Compatibility**: NinjaTrader 8
- **Framework**: .NET Framework (NinjaScript)
- **Calculate Mode**: OnBarClose for reliability

---

*This indicator is designed to identify potential harmonic pattern setups. Always use proper risk management and consider additional confirmation signals before making trading decisions.*