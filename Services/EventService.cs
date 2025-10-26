using EventEase.Models;
using Microsoft.JSInterop;
using System.Text.Json;

namespace EventEase.Services
{
    public interface IEventService
    {
        event Action? OnChange;
        Task<Event> AddEventAsync(Event evt);
        Task<List<Event>> GetEventsAsync();
        Task<Event?> GetEventByIdAsync(int id);
    }

    public class EventService : IEventService
    {
        private readonly IJSRuntime _jsRuntime;
        private const string StorageKey = "events";
        private const string LastIdKey = "lastEventId";
        private int _nextId = 1;
        private bool _isInitialized;
        private readonly SemaphoreSlim _initializationLock = new(1, 1);

        public EventService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public event Action? OnChange;

        private void NotifyStateChanged() => OnChange?.Invoke();

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

        private async Task SaveEventsAsync(List<Event> events)
        {
            var json = JsonSerializer.Serialize(events);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
        }

        private async Task SaveLastIdAsync()
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", LastIdKey, (_nextId - 1).ToString());
        }

        private async Task<List<Event>> LoadEventsAsync()
        {
            await EnsureInitializedAsync();
            
            var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", StorageKey);
            if (string.IsNullOrEmpty(json))
                return new List<Event>();

            try
            {
                return JsonSerializer.Deserialize<List<Event>>(json) ?? new List<Event>();
            }
            catch
            {
                return new List<Event>();
            }
        }

        public async Task<Event> AddEventAsync(Event evt)
        {
            await EnsureInitializedAsync();
            
            var events = await LoadEventsAsync();
            evt.Id = _nextId++;
            events.Add(evt);
            await SaveEventsAsync(events);
            await SaveLastIdAsync();
            NotifyStateChanged();
            return evt;
        }

        public async Task<List<Event>> GetEventsAsync()
        {
            await EnsureInitializedAsync();
            return await LoadEventsAsync();
        }

        public async Task<Event?> GetEventByIdAsync(int id)
        {
            await EnsureInitializedAsync();
            var events = await LoadEventsAsync();
            return events.FirstOrDefault(e => e.Id == id);
        }
    }
}