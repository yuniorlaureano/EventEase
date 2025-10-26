using EventEase.Models;
using Microsoft.JSInterop;
using System.Text.Json;

namespace EventEase.Services
{
    public interface IAttendanceService
    {
        Task<Attendance> RegisterAttendeeAsync(int eventId, Attendance attendance);
        Task<List<Attendance>> GetEventAttendeesAsync(int eventId);
        Task<Attendance?> GetAttendanceByIdAsync(int id);
        Task UpdateAttendanceStatusAsync(int id, AttendanceStatus status);
        event Action? OnChange;
    }

    public class AttendanceService : IAttendanceService
    {
        private readonly IJSRuntime _jsRuntime;
        private const string StorageKey = "attendances";
        private const string LastIdKey = "lastAttendanceId";
        private int _nextId = 1;
        private bool _isInitialized;
        private readonly SemaphoreSlim _initializationLock = new(1, 1);

        public event Action? OnChange;

        public AttendanceService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        private async Task EnsureInitializedAsync()
        {
            if (_isInitialized) return;

            await _initializationLock.WaitAsync();
            try
            {
                if (_isInitialized) return;

                var storedId = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", LastIdKey);
                if (int.TryParse(storedId, out int lastId))
                {
                    _nextId = lastId + 1;
                }

                _isInitialized = true;
            }
            finally
            {
                _initializationLock.Release();
            }
        }

        private void NotifyStateChanged() => OnChange?.Invoke();

        private async Task SaveAttendancesAsync(List<Attendance> attendances)
        {
            var json = JsonSerializer.Serialize(attendances);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
        }

        private async Task SaveLastIdAsync()
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", LastIdKey, (_nextId - 1).ToString());
        }

        private async Task<List<Attendance>> LoadAttendancesAsync()
        {
            var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", StorageKey);
            if (string.IsNullOrEmpty(json))
                return new List<Attendance>();

            try
            {
                return JsonSerializer.Deserialize<List<Attendance>>(json) ?? new List<Attendance>();
            }
            catch
            {
                return new List<Attendance>();
            }
        }

        public async Task<Attendance> RegisterAttendeeAsync(int eventId, Attendance attendance)
        {
            await EnsureInitializedAsync();

            var attendances = await LoadAttendancesAsync();
            attendance.Id = _nextId++;
            attendance.EventId = eventId;
            attendance.RegistrationDate = DateTime.Now;
            attendance.Status = AttendanceStatus.Registered;
            
            attendances.Add(attendance);
            await SaveAttendancesAsync(attendances);
            await SaveLastIdAsync();
            NotifyStateChanged();
            
            return attendance;
        }

        public async Task<List<Attendance>> GetEventAttendeesAsync(int eventId)
        {
            await EnsureInitializedAsync();
            var attendances = await LoadAttendancesAsync();
            return attendances.Where(a => a.EventId == eventId).ToList();
        }

        public async Task<Attendance?> GetAttendanceByIdAsync(int id)
        {
            await EnsureInitializedAsync();
            var attendances = await LoadAttendancesAsync();
            return attendances.FirstOrDefault(a => a.Id == id);
        }

        public async Task UpdateAttendanceStatusAsync(int id, AttendanceStatus status)
        {
            await EnsureInitializedAsync();
            var attendances = await LoadAttendancesAsync();
            var attendance = attendances.FirstOrDefault(a => a.Id == id);
            
            if (attendance != null)
            {
                attendance.Status = status;
                await SaveAttendancesAsync(attendances);
                NotifyStateChanged();
            }
        }
    }
}