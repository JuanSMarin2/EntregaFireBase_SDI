# EntregaFireBase_SDI
# Sistema de Autenticación y Leaderboard con Firebase

Este proyecto implementa un sistema completo de autenticación de usuarios y gestión de puntajes en Unity utilizando Firebase.

---

## Funcionalidades

### Autenticación

* Registro de usuarios con correo y contraseña
* Inicio de sesión
* Cierre de sesión
* Recuperación de contraseña mediante correo electrónico

### Gestión de usuario

* Almacenamiento de información del usuario (username, email y score)
* Persistencia básica de datos locales

### Leaderboard

* Guardado de puntajes en la base de datos
* Actualización automática del mejor puntaje por usuario
* Visualización de los mejores jugadores

---

## Funcionamiento

El sistema permite a los usuarios crear una cuenta o iniciar sesión para acceder al juego.
Cada usuario tiene un perfil asociado donde se almacena su nombre, correo y mejor puntaje.

Cuando se registra un nuevo score:

* Se guarda en la base de datos
* Se compara con el mejor puntaje actual del usuario
* Se actualiza solo si es superior

El leaderboard obtiene los datos desde la base de datos y muestra a los jugadores con mejores puntajes en orden descendente.

---

## Tecnologías utilizadas

* Unity (C#)
* Firebase Authentication
* Firebase Realtime Database
* TextMeshPro

---

## Descripción general

El sistema está diseñado para manejar autenticación y almacenamiento de datos en la nube de forma sencilla y eficiente, permitiendo mantener la información de los jugadores sincronizada y accesible en tiempo real.
