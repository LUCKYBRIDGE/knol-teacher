using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using KnolTeacher.Desktop.Models;
using KnolTeacher.Desktop.Services;
using KnolTeacher.Desktop.Views.Controls;

namespace KnolTeacher.Desktop.Views.Controls.Widgets;

public partial class WeatherWidgetView : UserControl, IWidgetLifecycle
{
    private static readonly TimeSpan AutoRefreshInterval = TimeSpan.FromMinutes(15);

    private readonly IWeatherService _weatherService;
    private readonly DispatcherTimer _autoRefreshTimer;
    private CancellationTokenSource? _refreshCts;
    private bool _isInitialized;
    private bool _isActive;
    private bool _disposed;

    public WeatherWidgetView(IWeatherService? weatherService = null)
    {
        _weatherService = weatherService ?? ((Application.Current as App)?.Services?.GetService(typeof(IWeatherService)) as IWeatherService)!;
        _autoRefreshTimer = new DispatcherTimer { Interval = AutoRefreshInterval };
        _autoRefreshTimer.Tick += async (_, _) =>
        {
            if (_isActive && !_disposed)
            {
                await BeginRefreshAsync();
            }
        };

        InitializeComponent();
    }

    public void Activate()
    {
        if (_disposed || _isActive) return;

        _isActive = true;
        if (!_isInitialized)
        {
            InitRegions();
            _isInitialized = true;
        }

        _autoRefreshTimer.Start();
        _ = BeginRefreshAsync();
    }

    public void Deactivate()
    {
        if (_disposed || !_isActive) return;

        _isActive = false;
        _autoRefreshTimer.Stop();
        CancelRefresh();
    }

    public void Dispose()
    {
        if (_disposed) return;

        Deactivate();
        _autoRefreshTimer.Stop();
        CancelRefresh();
        _disposed = true;
    }

    private void InitRegions()
    {
        try
        {
            CbRegion.Items.Clear();

            var schoolRegion = _weatherService.ResolveSchoolRegion();
            string schoolTag = $"🏫 {schoolRegion.Name} (우리학교)";
            CbRegion.Items.Add(schoolTag);

            if (_weatherService is WeatherService concreteService)
            {
                var localCities = concreteService.DetailedCityCoordinates
                    .Where(c => c.OfficeCode == schoolRegion.OfficeCode && c.Name != schoolRegion.Name)
                    .OrderBy(c => c.Name)
                    .ToList();
                foreach (var c in localCities)
                {
                    CbRegion.Items.Add(c.Name);
                }

                foreach (var r in concreteService.SupportedRegions)
                {
                    if (r.Name != schoolRegion.Name)
                    {
                        CbRegion.Items.Add(r.Name);
                    }
                }
            }

            CbRegion.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Nolboard.Weather] Region init failed: {ex.GetType().Name}");
        }
    }

    private async Task BeginRefreshAsync()
    {
        if (_disposed || !_isActive || _weatherService == null) return;

        CancelRefresh();
        _refreshCts = new CancellationTokenSource();
        var token = _refreshCts.Token;

        try
        {
            await RefreshWeatherAsync(token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // Hidden/closed widgets must not apply stale async results.
        }
    }

    private async Task RefreshWeatherAsync(CancellationToken cancellationToken)
    {
        try
        {
            string selectedRegion = CbRegion.SelectedItem as string ?? "서울";
            var weatherTask = _weatherService.GetWeatherAndAirQualityAsync(selectedRegion);
            var weather = await weatherTask.WaitAsync(cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            if (_disposed || !_isActive || weather == null)
            {
                if (weather == null && TxtUpdatedTime != null)
                {
                    TxtUpdatedTime.Text = "날씨 동기화 실패 · 잠시 후 다시 시도합니다";
                }
                return;
            }

            TxtWeatherIcon.Text = weather.WeatherIcon;
            TxtTemperature.Text = $"{weather.Temperature:0.0}°C";
            TxtWeatherDesc.Text = $"{weather.WeatherDescription} (체감 {weather.ApparentTemperature:0.0}°C)";
            TxtHumidity.Text = $"💧 습도 {weather.Humidity}%";
            TxtWind.Text = $"💨 풍속 {weather.WindSpeed:0.0}m/s";

            TxtPm10.Text = $"{weather.Pm10Grade} ({weather.Pm10:0}µg)";
            BdPm10.Background = (Brush)new BrushConverter().ConvertFromString(weather.Pm10BadgeBg)!;
            TxtPm10.Foreground = (Brush)new BrushConverter().ConvertFromString(weather.Pm10BadgeFg)!;

            TxtPm25.Text = $"{weather.Pm25Grade} ({weather.Pm25:0}µg)";
            BdPm25.Background = (Brush)new BrushConverter().ConvertFromString(weather.Pm25BadgeBg)!;
            TxtPm25.Foreground = (Brush)new BrushConverter().ConvertFromString(weather.Pm25BadgeFg)!;

            TxtOutdoorGuide.Text = weather.OutdoorActivityGuide;
            BdOutdoorGuide.Background = (Brush)new BrushConverter().ConvertFromString(weather.OutdoorGuideBg)!;
            TxtOutdoorGuide.Foreground = (Brush)new BrushConverter().ConvertFromString(weather.OutdoorGuideFg)!;

            if (TxtUpdatedTime != null)
            {
                string updated = string.IsNullOrWhiteSpace(weather.UpdatedTime)
                    ? DateTime.Now.ToString("HH:mm")
                    : weather.UpdatedTime;
                TxtUpdatedTime.Text = $"{updated} 기준 · 열린 동안 15분마다 자동 갱신";
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            if (TxtUpdatedTime != null)
            {
                TxtUpdatedTime.Text = "날씨 동기화 실패 · 기존 정보를 유지합니다";
            }
            System.Diagnostics.Debug.WriteLine($"[Nolboard.Weather] Refresh failed: {ex.GetType().Name}");
        }
    }

    private void CancelRefresh()
    {
        var cts = _refreshCts;
        _refreshCts = null;
        if (cts == null) return;

        try { cts.Cancel(); } catch { }
        cts.Dispose();
    }

    private async void CbRegion_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitialized && _isActive && !_disposed)
        {
            await BeginRefreshAsync();
        }
    }

    private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
    {
        if (_isActive && !_disposed)
        {
            await BeginRefreshAsync();
        }
    }
}
