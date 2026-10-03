using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace UNO
{
    public class ApiService
    {
        private readonly HttpClient cliente;

        public ApiService()
        {
            cliente = new HttpClient();
            cliente.BaseAddress = new Uri("http://127.0.0.1:8000/");
        }

        public async Task<int> CrearJugador(string nombre)
        {
            var datos = new
            {
                nombre = nombre
            };

            string json = JsonConvert.SerializeObject(datos);

            var contenido = new StringContent(
                json,
                Encoding.UTF8,
                "application/json"
            );

            HttpResponseMessage respuesta =
                await cliente.PostAsync("jugadores", contenido);

            respuesta.EnsureSuccessStatusCode();

            string respuestaJson =
                await respuesta.Content.ReadAsStringAsync();

            dynamic resultado =
                JsonConvert.DeserializeObject(respuestaJson);

            return (int)resultado.id_jugador;
        }


        public async Task<int> CrearPartida()
        {
            var contenido = new StringContent(
                "",
                Encoding.UTF8,
                "application/json"
            );

            HttpResponseMessage respuesta =
                await cliente.PostAsync("partidas", contenido);

            respuesta.EnsureSuccessStatusCode();

            string respuestaJson =
                await respuesta.Content.ReadAsStringAsync();

            dynamic resultado =
                JsonConvert.DeserializeObject(respuestaJson);

            return (int)resultado.id_partida;
        }


        public async Task GuardarJugada(
            int idPartida,
            int idJugador,
            int turno,
            string accion,
            string colorCarta,
            string valorCarta)
        {
            var datos = new
            {
                id_partida = idPartida,
                id_jugador = idJugador,
                turno = turno,
                accion = accion,
                color_carta = colorCarta,
                valor_carta = valorCarta
            };

            string json =
                JsonConvert.SerializeObject(datos);

            var contenido = new StringContent(
                json,
                Encoding.UTF8,
                "application/json"
            );

            HttpResponseMessage respuesta =
                await cliente.PostAsync(
                    "historial",
                    contenido
                );

            respuesta.EnsureSuccessStatusCode();
        }


        public async Task GuardarGanador(
            int idPartida,
            int idGanador)
        {
            var datos = new
            {
                id_ganador = idGanador
            };

            string json =
                JsonConvert.SerializeObject(datos);

            var contenido = new StringContent(
                json,
                Encoding.UTF8,
                "application/json"
            );

            HttpResponseMessage respuesta =
                await cliente.PutAsync(
                    $"partidas/{idPartida}/ganador",
                    contenido
                );

            respuesta.EnsureSuccessStatusCode();
        }
    }
}