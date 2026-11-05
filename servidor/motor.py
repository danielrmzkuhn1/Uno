"""Reglas de UNO: estado serializable, independiente de HTTP y MySQL."""
import random
import secrets
import time

COLORES = ('Rojo', 'Azul', 'Verde', 'Amarillo')


def carta(color, tipo='Numero', numero=-1):
    return dict(Color=color, Tipo=tipo, Numero=numero)


def nueva(nombre, jugador_id):
    return dict(jugadores=[jugador(nombre, jugador_id)], fase='espera', version=0,
                turno=0, direccion=1, mazo=[], descarte=[], color='Ninguno',
                ganador=None, uno=None, mensaje='Esperando jugadores', eventos=[], solicitudes=[])


def jugador(nombre, jugador_id):
    return dict(nombre=nombre, id=jugador_id, token=secrets.token_urlsafe(32), mano=[])


def revancha(s):
    if s['fase'] != 'terminada':
        raise ValueError('Solo puedes iniciar otra partida cuando termine la actual')
    return dict(jugadores=[dict(nombre=j['nombre'], id=j['id'], token=j['token'], mano=[])
                           for j in s['jugadores']],
                fase='espera', version=s['version'] + 1, turno=0, direccion=1,
                mazo=[], descarte=[], color='Ninguno', ganador=None, uno=None,
                mensaje='Nueva partida. Esperando al creador', eventos=[], solicitudes=[])


def evento(s, indice, accion, c=None):
    s['eventos'].append(dict(jugador=s['jugadores'][indice]['id'], accion=accion,
                             color=c['Color'] if c else None,
                             valor=(str(c['Numero']) if c['Tipo'] == 'Numero' else c['Tipo']) if c else None))


def validar_entrada(s, nombre):
    if s['fase'] != 'espera' or len(s['jugadores']) >= 4:
        raise ValueError('La sala ya inició o está llena')
    if any(j['nombre'].casefold() == nombre.casefold() for j in s['jugadores']):
        raise ValueError('Ese nombre ya está en la sala')


def entrar(s, nombre, jugador_id):
    validar_entrada(s, nombre)
    s['jugadores'].append(jugador(nombre, jugador_id))
    s['version'] += 1
    return len(s['jugadores']) - 1


def robar(s, indice, cantidad):
    for _ in range(cantidad):
        if not s['mazo'] and len(s['descarte']) > 1:
            s['mazo'] = s['descarte'][:-1]
            s['descarte'] = s['descarte'][-1:]
            random.shuffle(s['mazo'])
        if not s['mazo']:
            break
        s['jugadores'][indice]['mano'].append(s['mazo'].pop())


def iniciar(s, indice):
    if indice != 0 or s['fase'] != 'espera' or len(s['jugadores']) < 2:
        raise ValueError('Solo el creador puede iniciar, con 2 a 4 jugadores')
    mazo = []
    for color in COLORES:
        mazo.append(carta(color, numero=0))
        for n in range(1, 10):
            mazo.extend([carta(color, numero=n), carta(color, numero=n)])
        for tipo in ('Saltar', 'Reversa', 'MasDos'):
            mazo.extend([carta(color, tipo), carta(color, tipo)])
    for _ in range(4):
        mazo.extend([carta('Ninguno', 'Comodin'), carta('Ninguno', 'MasCuatro')])
    random.shuffle(mazo)
    s['mazo'] = mazo
    for _ in range(7):
        for i in range(len(s['jugadores'])):
            robar(s, i, 1)
    while s['mazo'][-1]['Tipo'] != 'Numero':
        s['mazo'].insert(0, s['mazo'].pop())
    s['descarte'] = [s['mazo'].pop()]
    s['color'] = s['descarte'][0]['Color']
    s['fase'] = 'jugando'
    s['mensaje'] = 'Partida iniciada'
    evento(s, indice, 'Inicio')


def jugable(s, c):
    superior = s['descarte'][-1]
    return (c['Tipo'] in ('Comodin', 'MasCuatro') or c['Color'] == s['color'] or
            (c['Tipo'] == superior['Tipo'] and
             (c['Tipo'] != 'Numero' or c['Numero'] == superior['Numero'])))


def expirar_uno(s, ahora=None):
    ahora = time.time() if ahora is None else ahora
    if s['uno'] and ahora >= s['uno']['hasta']:
        i = s['uno']['jugador']
        robar(s, i, 2)
        evento(s, i, 'Castigo UNO')
        s['uno'] = None
        s['mensaje'] = s['jugadores'][i]['nombre'] + ' olvidó decir UNO y recibe castigo'
        s['version'] += 1
        return True
    return False


def actuar(s, indice, accion, numero=None, color=None):
    if accion == 'iniciar':
        iniciar(s, indice)
        return
    if s['fase'] != 'jugando':
        raise ValueError('La partida no está en curso')
    if accion == 'uno':
        if not s['uno'] or s['uno']['jugador'] != indice:
            raise ValueError('No tienes una declaración de UNO pendiente')
        s['uno'] = None
        s['mensaje'] = s['jugadores'][indice]['nombre'] + ' dijo ¡UNO!'
        evento(s, indice, 'Dijo UNO')
        return
    if s['uno']:
        raise ValueError('Espera a que termine la ventana de UNO')
    if indice != s['turno']:
        raise ValueError('No es tu turno')
    n = len(s['jugadores'])
    if accion == 'robar':
        robar(s, indice, 1)
        s['turno'] = (indice + s['direccion']) % n
        s['mensaje'] = s['jugadores'][indice]['nombre'] + ' robó una carta'
        evento(s, indice, 'Robar')
        return
    if accion != 'jugar':
        raise ValueError('Acción desconocida')
    mano = s['jugadores'][indice]['mano']
    if numero is None or numero < 0 or numero >= len(mano):
        raise ValueError('Carta inexistente')
    c = mano[numero]
    if not jugable(s, c):
        raise ValueError('Esa carta no se puede jugar')
    comodin = c['Tipo'] in ('Comodin', 'MasCuatro')
    if comodin and color not in COLORES:
        raise ValueError('Elige un color válido')
    mano.pop(numero)
    s['descarte'].append(c)
    s['color'] = color if comodin else c['Color']
    evento(s, indice, 'Jugar', c)
    s['mensaje'] = s['jugadores'][indice]['nombre'] + ' jugó una carta'
    if not mano:
        s['fase'] = 'terminada'
        s['ganador'] = indice
        evento(s, indice, 'Gano')
        return
    pasos = 1
    if c['Tipo'] == 'Reversa':
        s['direccion'] *= -1
        pasos = 2 if n == 2 else 1
    elif c['Tipo'] == 'Saltar':
        pasos = 2
    elif c['Tipo'] in ('MasDos', 'MasCuatro'):
        victima = (indice + s['direccion']) % n
        robar(s, victima, 2 if c['Tipo'] == 'MasDos' else 4)
        pasos = 2
    s['turno'] = (indice + s['direccion'] * pasos) % n
    if len(mano) == 1:
        s['uno'] = dict(jugador=indice, hasta=time.time() + 5)


def vista(s, indice, codigo):
    return dict(codigo=codigo, version=s['version'], fase=s['fase'], yo=indice,
                turno=s['turno'], horario=s['direccion'] == 1, color=s['color'],
                superior=s['descarte'][-1] if s['descarte'] else None,
                mazo=len(s['mazo']), ganador=s['ganador'], mensaje=s['mensaje'],
                uno_jugador=s['uno']['jugador'] if s['uno'] else -1,
                uno_segundos=max(0, s['uno']['hasta'] - time.time()) if s['uno'] else 0,
                jugadores=[dict(nombre=j['nombre'], cantidad=len(j['mano'])) for j in s['jugadores']],
                mano=s['jugadores'][indice]['mano'],
                jugables=[i for i, c in enumerate(s['jugadores'][indice]['mano'])
                          if s['fase'] == 'jugando' and not s['uno'] and indice == s['turno'] and jugable(s, c)])
