using System;
using System.Collections.Generic;
using System.Text;

namespace Blackjack21
{
    // Una carta conoce su número, su palo y cómo se representa.
    public sealed class Carta
    {
        public int Numero { get; }
        public char Palo { get; }

        public Carta(int numero, char palo)
        {
            if (numero < 1 || numero > 13)
                throw new ArgumentOutOfRangeException(nameof(numero));

            Numero = numero;
            Palo = palo;
        }

        public string Etiqueta
        {
            get
            {
                switch (Numero)
                {
                    case 1: return "A";
                    case 11: return "J";
                    case 12: return "Q";
                    case 13: return "K";
                    default: return Numero.ToString();
                }
            }
        }

        public int ValorBase => Numero == 1 ? 1 : Math.Min(Numero, 10);
    }

    // Baraja francesa de 52 cartas, mezclada con Fisher-Yates.
    public sealed class Mazo
    {
        private readonly List<Carta> _cartas = new List<Carta>();
        private int _siguiente;

        public Mazo()
        {
            foreach (char palo in new[] { '♥', '♦', '♣', '♠' })
                for (int numero = 1; numero <= 13; numero++)
                    _cartas.Add(new Carta(numero, palo));

            for (int i = _cartas.Count - 1; i > 0; i--)
            {
                int j = Random.Shared.Next(i + 1);
                Carta temporal = _cartas[i];
                _cartas[i] = _cartas[j];
                _cartas[j] = temporal;
            }
        }

        public Carta SacarCarta()
        {
            if (_siguiente >= _cartas.Count)
                throw new InvalidOperationException("No quedan cartas en el mazo.");

            return _cartas[_siguiente++];
        }
    }

    // Calcula el as como 1 u 11, eligiendo el valor más favorable <= 21.
    public sealed class Mano
    {
        private readonly List<Carta> _cartas = new List<Carta>();
        public IReadOnlyList<Carta> Cartas => _cartas;

        public void Agregar(Carta carta) => _cartas.Add(carta);

        public int Puntos
        {
            get
            {
                int puntos = 0;
                int ases = 0;

                foreach (Carta carta in _cartas)
                {
                    puntos += carta.ValorBase;
                    if (carta.Numero == 1)
                        ases++;
                }

                // Primero todos los ases valen 1; luego convertimos uno en 11 si cabe.
                while (ases > 0 && puntos + 10 <= 21)
                {
                    puntos += 10;
                    ases--;
                }

                return puntos;
            }
        }

        public bool SePasa => Puntos > 21;
        public bool EsBlackjack => _cartas.Count == 2 && Puntos == 21;
    }

    // Herencia: ambos participantes comparten nombre y mano.
    public abstract class Participante
    {
        public string Nombre { get; }
        public Mano Mano { get; } = new Mano();

        protected Participante(string nombre) => Nombre = nombre;
        public void RecibirCarta(Mazo mazo) => Mano.Agregar(mazo.SacarCarta());
    }

    public sealed class Jugador : Participante
    {
        public Jugador() : base("JUGADOR") { }
    }

    public sealed class Crupier : Participante
    {
        public Crupier() : base("CRUPIER") { }

        // Se planta en 17, incluyendo el 17 blando.
        public bool DebePedirCarta => Mano.Puntos < 17;
    }

    public enum Resultado { Victoria, Derrota, Empate }

    // Responsable exclusivamente de presentar el juego por consola.
    public static class VistaConsola
    {
        private const int AnchoCabecera = 62;
        private const int AnchoCarta = 9;

        public static void Dibujar(Jugador jugador, Crupier crupier,
                                   bool revelarCrupier, int victorias,
                                   int derrotas, int empates)
        {
            if (!Console.IsOutputRedirected)
                Console.Clear();

            Console.WriteLine("+" + new string('-', AnchoCabecera - 2) + "+");
            Linea("  +-------+   JUEGO: Blackjack 21");
            Linea("  |K      |   LENGUAJE: C#");
            Linea("  |   ♥   |   PROGRAMACIÓN Y MOTORES");
            Linea("  |      K|   OBJETIVO: ACERCARSE A 21");
            Linea("  +-------+   SIN PASARSE");
            Console.WriteLine("+" + new string('-', AnchoCabecera - 2) + "+");
            Console.WriteLine($"  VICTORIAS: {victorias}   DERROTAS: {derrotas}   EMPATES: {empates}");
            Console.WriteLine(new string('-', AnchoCabecera));

            Console.WriteLine("MANO DEL CRUPIER:");
            MostrarCartas(crupier.Mano.Cartas, !revelarCrupier);
            Console.WriteLine(revelarCrupier
                ? $"PUNTOS DEL CRUPIER: {crupier.Mano.Puntos}"
                : "PUNTOS DEL CRUPIER: ? (segunda carta oculta)");

            Console.WriteLine(new string('-', AnchoCabecera));
            Console.WriteLine("MANO DEL JUGADOR:");
            MostrarCartas(jugador.Mano.Cartas, false);
            Console.WriteLine($"PUNTOS DEL JUGADOR: {jugador.Mano.Puntos}");
            Console.WriteLine(new string('-', AnchoCabecera));
        }

        private static void Linea(string texto)
        {
            Console.WriteLine("|" + texto.PadRight(AnchoCabecera - 2) + "|");
        }

        private static void MostrarCartas(IReadOnlyList<Carta> cartas, bool ocultarSegunda)
        {
            int anchoConsola = 80;
            try
            {
                if (!Console.IsOutputRedirected)
                    anchoConsola = Math.Max(AnchoCarta, Console.WindowWidth - 2);
            }
            catch (System.IO.IOException) { /* terminal sin tamaño disponible */ }

            int porFila = Math.Max(1, anchoConsola / (AnchoCarta + 1));

            for (int inicio = 0; inicio < cartas.Count; inicio += porFila)
            {
                int fin = Math.Min(inicio + porFila, cartas.Count);

                for (int linea = 0; linea < 5; linea++)
                {
                    var partes = new List<string>();
                    for (int i = inicio; i < fin; i++)
                    {
                        bool bocaAbajo = ocultarSegunda && i == 1;
                        partes.Add(DibujarCarta(cartas[i], linea, bocaAbajo));
                    }
                    Console.WriteLine(string.Join(" ", partes));
                }
            }
        }

        private static string DibujarCarta(Carta carta, int linea, bool bocaAbajo)
        {
            if (linea == 0 || linea == 4)
                return "+-------+";

            if (bocaAbajo)
            {
                if (linea == 2) return "|## ? ##|";
                return "|#######|";
            }

            if (linea == 1) return "|" + carta.Etiqueta.PadRight(7) + "|";
            if (linea == 2) return "|   " + carta.Palo + "   |";
            return "|" + carta.Etiqueta.PadLeft(7) + "|";
        }

        public static bool PreguntarSiNo(string pregunta)
        {
            while (true)
            {
                Console.Write(pregunta + " [s/n]: ");
                string? entrada = Console.ReadLine();
                if (entrada == null) return false;

                switch (entrada.Trim().ToLowerInvariant())
                {
                    case "s":
                    case "si":
                    case "sí":
                    case "y":
                    case "yes":
                        return true;
                    case "n":
                    case "no":
                        return false;
                    default:
                        Console.WriteLine("Respuesta no válida. Escribe s o n.");
                        break;
                }
            }
        }
    }

    // Controla el reparto, los turnos, las reglas y el marcador.
    public sealed class JuegoBlackjack
    {
        private int _victorias;
        private int _derrotas;
        private int _empates;

        public void Ejecutar()
        {
            try
            {
                bool continuar;
                do
                {
                    Resultado resultado = JugarRonda();
                    Registrar(resultado);
                    continuar = VistaConsola.PreguntarSiNo("¿Quieres jugar otra partida?");
                } while (continuar);

                Console.WriteLine($"\nMarcador final: {_victorias} victorias, " +
                                  $"{_derrotas} derrotas y {_empates} empates. ¡Hasta pronto!");
            }
            catch (System.IO.IOException ex)
            {
                Console.Error.WriteLine("Error de entrada/salida: " + ex.Message);
            }
        }

        private Resultado JugarRonda()
        {
            var mazo = new Mazo();
            var jugador = new Jugador();
            var crupier = new Crupier();

            // Reparto alterno: dos cartas a cada uno.
            jugador.RecibirCarta(mazo);
            crupier.RecibirCarta(mazo);
            jugador.RecibirCarta(mazo);
            crupier.RecibirCarta(mazo);

            // Los blackjack naturales se resuelven antes de pedir cartas.
            if (jugador.Mano.EsBlackjack || crupier.Mano.EsBlackjack)
            {
                MostrarEstado(jugador, crupier, true);
                if (jugador.Mano.EsBlackjack && crupier.Mano.EsBlackjack)
                    return Finalizar(Resultado.Empate, "¡Ambos tienen Blackjack! EMPATE (push).");
                if (jugador.Mano.EsBlackjack)
                    return Finalizar(Resultado.Victoria, "¡BLACKJACK! ¡GANA EL JUGADOR!");
                return Finalizar(Resultado.Derrota, "¡Blackjack del crupier! GANA EL CRUPIER.");
            }

            // Turno del jugador.
            while (jugador.Mano.Puntos < 21)
            {
                MostrarEstado(jugador, crupier, false);
                if (!VistaConsola.PreguntarSiNo("¿Deseas coger otra carta?"))
                    break;

                jugador.RecibirCarta(mazo);
            }

            if (jugador.Mano.SePasa)
            {
                MostrarEstado(jugador, crupier, true);
                return Finalizar(Resultado.Derrota, "¡Te has pasado de 21! GANA EL CRUPIER.");
            }

            // El crupier destapa su carta y pide automáticamente hasta 17.
            while (crupier.DebePedirCarta)
                crupier.RecibirCarta(mazo);

            MostrarEstado(jugador, crupier, true);
            return CompararManos(jugador.Mano, crupier.Mano);
        }

        private void MostrarEstado(Jugador jugador, Crupier crupier, bool revelar)
        {
            VistaConsola.Dibujar(jugador, crupier, revelar,
                                _victorias, _derrotas, _empates);
        }

        private static Resultado CompararManos(Mano jugador, Mano crupier)
        {
            if (crupier.SePasa)
                return Finalizar(Resultado.Victoria, "¡El crupier se ha pasado! GANA EL JUGADOR.");

            if (jugador.Puntos > crupier.Puntos)
                return Finalizar(Resultado.Victoria, "¡GANA EL JUGADOR!");

            if (crupier.Puntos > jugador.Puntos)
                return Finalizar(Resultado.Derrota, "¡GANA EL CRUPIER!");

            // Regla específica del ejercicio: en empate a 21,
            // gana el crupier si ha usado MENOS cartas.
            if (jugador.Puntos == 21 && crupier.Cartas.Count < jugador.Cartas.Count)
                return Finalizar(Resultado.Derrota,
                    "¡EMPATE A 21! Ventaja del crupier: tiene menos cartas. GANA EL CRUPIER.");

            return Finalizar(Resultado.Empate, "¡EMPATE! (push).");
        }

        private static Resultado Finalizar(Resultado resultado, string mensaje)
        {
            Console.WriteLine("\n" + mensaje + "\n");
            return resultado;
        }

        private void Registrar(Resultado resultado)
        {
            switch (resultado)
            {
                case Resultado.Victoria: _victorias++; break;
                case Resultado.Derrota: _derrotas++; break;
                default: _empates++; break;
            }
        }
    }

    public static class Program
    {
        public static void Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            new JuegoBlackjack().Ejecutar();
        }
    }
}
