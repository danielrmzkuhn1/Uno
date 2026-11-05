import copy
import random
import unittest
from unittest.mock import patch
import motor


class MotorTests(unittest.TestCase):
    def partida(self, n=4):
        s = motor.nueva('Ana', 1)
        for i in range(1, n):
            motor.entrar(s, 'Jugador ' + str(i), i + 1)
        motor.iniciar(s, 0)
        return s

    def test_108_cartas_y_reparto(self):
        for n in (2, 3, 4):
            s = self.partida(n)
            self.assertEqual(108, len(s['mazo']) + len(s['descarte']) + sum(len(j['mano']) for j in s['jugadores']))
            self.assertTrue(all(len(j['mano']) == 7 for j in s['jugadores']))
            self.assertEqual('Numero', s['descarte'][-1]['Tipo'])

    def test_sala_llena_y_nombres(self):
        s = motor.nueva('Ana', 1)
        with self.assertRaises(ValueError): motor.entrar(s, 'ANA', 2)
        for i in range(3): motor.entrar(s, str(i), i + 2)
        with self.assertRaises(ValueError): motor.entrar(s, 'Quinto', 5)

    def test_inicio_solo_anfitrion_con_dos(self):
        s = motor.nueva('Ana', 1)
        with self.assertRaises(ValueError): motor.iniciar(s, 0)
        motor.entrar(s, 'Luis', 2)
        with self.assertRaises(ValueError): motor.iniciar(s, 1)
        motor.iniciar(s, 0)
        with self.assertRaises(ValueError): motor.entrar(s, 'Tarde', 3)

    def test_revancha_reinicia_y_conserva_identidades(self):
        s = self.partida(2)
        s['fase'] = 'terminada'
        s['ganador'] = 0
        s['version'] = 8
        tokens = [j['token'] for j in s['jugadores']]
        ids = [j['id'] for j in s['jugadores']]

        nueva = motor.revancha(s)

        self.assertEqual('espera', nueva['fase'])
        self.assertEqual(9, nueva['version'])
        self.assertIsNone(nueva['ganador'])
        self.assertEqual(ids, [j['id'] for j in nueva['jugadores']])
        self.assertEqual(tokens, [j['token'] for j in nueva['jugadores']])
        self.assertTrue(all(not j['mano'] for j in nueva['jugadores']))
        with self.assertRaises(ValueError): motor.revancha(nueva)

    def test_turno_invalido_no_muta_estado(self):
        s = self.partida()
        antes = copy.deepcopy(s)
        with self.assertRaises(ValueError): motor.actuar(s, 1, 'robar')
        self.assertEqual(antes, s)

    def test_robar_registra_actor_y_cambia_turno(self):
        s = self.partida()
        motor.actuar(s, 0, 'robar')
        self.assertEqual(1, s['turno'])
        self.assertEqual(1, s['eventos'][-1]['jugador'])
        self.assertEqual(8, len(s['jugadores'][0]['mano']))

    def test_privacidad(self):
        s = self.partida()
        v = motor.vista(s, 1, 'CODIGO')
        self.assertNotIn('token', str(v))
        self.assertNotIn('mano', str(v['jugadores']))
        self.assertEqual(s['jugadores'][1]['mano'], v['mano'])

    def preparar(self, tipo, n=4):
        s = self.partida(n)
        s['jugadores'][0]['mano'] = [motor.carta('Rojo', tipo), motor.carta('Azul', numero=7), motor.carta('Azul', numero=8)]
        s['color'] = 'Rojo'
        return s

    def test_saltar(self):
        s = self.preparar('Saltar')
        motor.actuar(s, 0, 'jugar', 0)
        self.assertEqual(2, s['turno'])

    def test_reversa_dos_y_cuatro(self):
        for n, siguiente in ((2, 0), (4, 3)):
            s = self.preparar('Reversa', n)
            motor.actuar(s, 0, 'jugar', 0)
            self.assertEqual(-1, s['direccion'])
            self.assertEqual(siguiente, s['turno'])

    def test_castigos(self):
        for tipo, cantidad in (('MasDos', 2), ('MasCuatro', 4)):
            s = self.preparar(tipo)
            motor.actuar(s, 0, 'jugar', 0, 'Verde')
            self.assertEqual(7 + cantidad, len(s['jugadores'][1]['mano']))
            self.assertEqual(2, s['turno'])

    def test_comodin_requiere_color(self):
        s = self.preparar('Comodin')
        antes = copy.deepcopy(s)
        with self.assertRaises(ValueError): motor.actuar(s, 0, 'jugar', 0, 'Ninguno')
        self.assertEqual(antes, s)

    def test_uno_ventana_y_castigo(self):
        s = self.preparar('Numero')
        s['jugadores'][0]['mano'].pop()
        with patch('motor.time.time', return_value=100): motor.actuar(s, 0, 'jugar', 0)
        with self.assertRaises(ValueError): motor.actuar(s, 1, 'robar')
        self.assertFalse(motor.expirar_uno(s, 104.9))
        self.assertTrue(motor.expirar_uno(s, 105))
        self.assertEqual(3, len(s['jugadores'][0]['mano']))
        self.assertFalse(motor.expirar_uno(s, 106))

    def test_uno_solo_propietario(self):
        s = self.preparar('Numero')
        s['jugadores'][0]['mano'].pop()
        motor.actuar(s, 0, 'jugar', 0)
        with self.assertRaises(ValueError): motor.actuar(s, 1, 'uno')
        motor.actuar(s, 0, 'uno')
        self.assertIsNone(s['uno'])
        self.assertEqual(1, s['eventos'][-1]['jugador'])

    def test_ganador(self):
        s = self.preparar('Numero')
        s['jugadores'][0]['mano'] = s['jugadores'][0]['mano'][:1]
        motor.actuar(s, 0, 'jugar', 0)
        self.assertEqual('terminada', s['fase'])
        self.assertEqual(0, s['ganador'])
        with self.assertRaises(ValueError): motor.actuar(s, 0, 'robar')

    def test_reciclaje_conserva_tope(self):
        s = self.partida()
        tope = s['descarte'][-1]
        s['descarte'] = s['mazo'] + [tope]
        s['mazo'] = []
        motor.robar(s, 0, 1)
        self.assertEqual([tope], s['descarte'])
        self.assertEqual(108, len(s['mazo']) + 1 + sum(len(j['mano']) for j in s['jugadores']))

    def test_simulacion_conserva_cartas(self):
        random.seed(42)
        for n in (2, 3, 4):
            s = self.partida(n)
            for _ in range(2500):
                if s['fase'] == 'terminada': break
                if s['uno']: motor.actuar(s, s['uno']['jugador'], 'uno')
                i = s['turno']
                disponibles = [k for k, c in enumerate(s['jugadores'][i]['mano']) if motor.jugable(s, c)]
                if disponibles: motor.actuar(s, i, 'jugar', disponibles[0], 'Rojo')
                else: motor.actuar(s, i, 'robar')
                self.assertEqual(108, len(s['mazo']) + len(s['descarte']) + sum(len(j['mano']) for j in s['jugadores']))
            self.assertEqual('terminada', s['fase'])


if __name__ == '__main__':
    unittest.main()
