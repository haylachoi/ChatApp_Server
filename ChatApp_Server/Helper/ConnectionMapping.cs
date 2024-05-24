using ChatApp_Server.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace ChatApp_Server.Helper
{
    public class ConnectionMapping<T> where T: notnull
    {
        private readonly Dictionary<T, HashSet<string>> _connections =
            new Dictionary<T, HashSet<string>>();

        public int Count
        {
            get
            {
                return _connections.Count;
            }
        }

        public void Add(T key, string connectionId)
        {
            lock (_connections)
            {
                HashSet<string> connections;
                if (!_connections.TryGetValue(key, out connections!))
                {
                    connections = new HashSet<string>();
                    _connections.Add(key, connections);
                }

                lock (connections)
                {
                    connections.Add(connectionId);
                }
            }
        }

        public IEnumerable<string> GetConnections(T key)
        {
            HashSet<string> connections;
            if (_connections.TryGetValue(key, out connections!))
            {
                return connections;
            }

            return Enumerable.Empty<string>();
        }

        public void Remove(T key, string connectionId)
        {
            lock (_connections)
            {
                HashSet<string> connections;
                if (!_connections.TryGetValue(key, out connections!))
                {
                    return;
                }

                lock (connections)
                {
                    connections.Remove(connectionId);

                    if (connections.Count == 0)
                    {
                        _connections.Remove(key);
                    }
                }
            }
        }
        public async Task AddConnectionToGroup(string groupname, T userId, IHubContext<ClientHub> hubContext)
        {
            var connections = GetConnections(userId);
            if (connections == null)
            {
                return;
            }

            List<Task> addToGroupTaskList = new List<Task>();
            foreach (var connection in connections)
            {
                addToGroupTaskList.Add(hubContext.Groups.AddToGroupAsync(connection, groupname));
            }
            await Task.WhenAll(addToGroupTaskList);
        }
        public async Task RemoveConnectionFromGroup(string groupname, T userId, IHubContext<ClientHub> hubContext)
        {
            var connections = GetConnections(userId);
            if (connections == null)
            {
                return;
            }

            List<Task> addToGroupTaskList = new List<Task>();
            foreach (var connection in connections)
            {
                addToGroupTaskList.Remove(hubContext.Groups.AddToGroupAsync(connection, groupname));
            }
            await Task.WhenAll(addToGroupTaskList);
        }
    }
}
