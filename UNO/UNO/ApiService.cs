using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UNO
{
    // Nombre conservado por compatibilidad; el transporte es TCP, no HTTP.
    public sealed class ApiService : IDisposable
    {
        private readonly string host;
        private readonly int puerto;
        private readonly TcpClient cliente = new TcpClient();
        private readonly SemaphoreSlim escritura = new SemaphoreSlim(1, 1);
        private readonly ConcurrentDictionary<string, TaskCompletionSource<JObject>> pendientes = new ConcurrentDictionary<string, TaskCompletionSource<JObject>>();
        private StreamReader lector;
        private StreamWriter escritor;
        private bool cerrado;
        public JObject UltimoEstado { get; private set; }
        public event Action<JObject> EstadoRecibido;
        public event Action<string> Desconectado;

        public ApiService(string direccion)
        {
            Uri uri;
            if (!Uri.TryCreate("tcp://" + direccion.Trim().Replace("tcp://", ""), UriKind.Absolute, out uri)
                || uri.Port < 1 || uri.Port > 65535 || (uri.AbsolutePath != "/" && uri.AbsolutePath != "") || !String.IsNullOrEmpty(uri.UserInfo))
                throw new ArgumentException("Usa IP:puerto, por ejemplo 192.168.1.20:8000");
            host = uri.Host;
            puerto = uri.Port;
        }

        public async Task<JObject> Entrar(string nombre, string sala, bool crear)
        {
            var conectar = cliente.ConnectAsync(host, puerto);
            if (await Task.WhenAny(conectar, Task.Delay(8000)) != conectar)
            { Dispose(); throw new TimeoutException("El servidor no respondió"); }
            await conectar;
            cliente.NoDelay = true;
            var red = cliente.GetStream();
            lector = new StreamReader(red, new UTF8Encoding(false, true), false, 4096, true);
            escritor = new StreamWriter(red, new UTF8Encoding(false), 4096, true) { AutoFlush = true, NewLine = "\n" };
            var lectura = Task.Run(Leer);
            var respuesta = await Enviar(new JObject
            {
                ["tipo"] = crear ? "crear" : "entrar",
                ["nombre"] = nombre,
                ["codigo"] = sala.Trim().ToUpperInvariant()
            });
            return (JObject)respuesta["estado"];
        }

        private async Task Leer()
        {
            try
            {
                while (!cerrado)
                {
                    string linea = await lector.ReadLineAsync().ConfigureAwait(false);
                    if (linea == null) throw new IOException("El servidor cerró la conexión");
                    var mensaje = JObject.Parse(linea);
                    if (mensaje["estado"] is JObject) UltimoEstado = (JObject)mensaje["estado"];
                    if ((string)mensaje["tipo"] == "estado")
                    {
                        var evento = EstadoRecibido;
                        if (evento != null) evento((JObject)mensaje["estado"]);
                    }
                    else
                    {
                        TaskCompletionSource<JObject> solicitud;
                        if (pendientes.TryRemove((string)mensaje["id"] ?? "", out solicitud))
                        {
                            if ((bool)mensaje["ok"]) solicitud.TrySetResult(mensaje);
                            else solicitud.TrySetException(new InvalidOperationException((string)mensaje["error"]));
                        }
                    }
                }
            }
            catch (Exception ex) { Terminar(ex.Message); }
        }

        private async Task<JObject> Enviar(JObject mensaje)
        {
            if (cerrado) throw new IOException("Conexión cerrada. Abre de nuevo el juego para crear otra sala.");
            string id = Guid.NewGuid().ToString("N");
            mensaje["id"] = id;
            var respuesta = new TaskCompletionSource<JObject>(TaskCreationOptions.RunContinuationsAsynchronously);
            pendientes[id] = respuesta;
            try
            {
                await escritura.WaitAsync();
                try { await escritor.WriteLineAsync(mensaje.ToString(Formatting.None)); }
                finally { escritura.Release(); }
                if (await Task.WhenAny(respuesta.Task, Task.Delay(10000)) != respuesta.Task)
                    Terminar("Tiempo de espera agotado. La acción pudo llegar al servidor.");
                return await respuesta.Task;
            }
            finally
            {
                TaskCompletionSource<JObject> retirado;
                pendientes.TryRemove(id, out retirado);
            }
        }

        public async Task<JObject> Consultar()
        { return (JObject)(await Enviar(new JObject { ["tipo"] = "consultar" }))["estado"]; }

        public async Task<JObject> Actuar(string accion, int version, int? carta = null, string color = null)
        {
            var mensaje = new JObject
            {
                ["tipo"] = "accion",
                ["datos"] = JObject.FromObject(new
                {
                    accion,
                    version,
                    carta,
                    color,
                    solicitud = Guid.NewGuid().ToString("N")
                })
            };
            return (JObject)(await Enviar(mensaje))["estado"];
        }
        public async Task<JObject> Cancelar()
        {
            return (JObject)(await Enviar(new JObject
            {
                ["tipo"] = "cancelar"
            }))["estado"];
        }

        public async Task Salir()
        {
            await Enviar(new JObject
            {
                ["tipo"] = "salir"
            });
        }


        private void Terminar(string motivo)
        {
            if (cerrado) return;
            cerrado = true;
            cliente.Close();
            foreach (var p in pendientes.Values) p.TrySetException(new IOException(motivo));
            var evento = Desconectado;
            if (evento != null) evento(motivo);
        }
        public void Dispose() { Terminar("Conexión cerrada"); }
    }
}
