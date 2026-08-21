# Clínica - microservicios con Docker

Proyecto académico compuesto por dos API, un API Gateway, SQL Server y RabbitMQ. Todo se construye, configura e inicia desde un único archivo `docker-compose.yml`; no es necesario instalar .NET ni SQL Server en el equipo anfitrión.

## Componentes

| Componente | Dirección o puerto |
|---|---|
| API Gateway | http://localhost:5100 |
| API de pacientes / Swagger | http://localhost:5101/swagger |
| API de historial / Swagger | http://localhost:5102/swagger |
| RabbitMQ Management | http://localhost:15672 |
| SQL Server | `localhost,1433` |

El Gateway publica las rutas `/pacientes` y `/historiales`. Las API se comunican mediante el exchange de RabbitMQ `clinica.events`.

## Requisitos

1. Instalar [Docker Desktop](https://www.docker.com/products/docker-desktop/).
2. Iniciar Docker Desktop y esperar hasta que el motor esté funcionando.
3. Clonar o descargar este repositorio.
4. Verificar que los puertos `1433`, `5672`, `15672`, `5100`, `5101` y `5102` estén libres.

## Ejecución paso a paso

Abra PowerShell o una terminal dentro de la carpeta que contiene `docker-compose.yml` y ejecute:

```powershell
docker compose up --build -d
```

Este único comando descarga las imágenes, compila las tres aplicaciones, espera a SQL Server y RabbitMQ, ejecuta `database/init.sql`, crea las dos bases y finalmente inicia todo el sistema.

Compruebe el estado de los contenedores:

```powershell
docker compose ps
```

El contenedor `clinica-database-init` debe aparecer como `Exited (0)`. Esto es correcto: termina después de crear las bases.

Pruebe el Gateway:

```powershell
Invoke-RestMethod http://localhost:5100/health
Invoke-RestMethod http://localhost:5100/pacientes
Invoke-RestMethod http://localhost:5100/historiales
```

También puede abrir Swagger:

- Pacientes: http://localhost:5101/swagger
- Historial clínico: http://localhost:5102/swagger

## Credenciales de desarrollo

- SQL Server: usuario `sa`, contraseña `Clinica_2026!`.
- RabbitMQ: usuario `admin`, contraseña `root12345`.

Para cambiarlas sin editar el Compose, cree un archivo `.env` junto a `docker-compose.yml`:

```dotenv
SQL_SA_PASSWORD=UnaClaveSegura_2026!
RABBITMQ_USER=admin
RABBITMQ_PASSWORD=OtraClaveSegura_2026!
```

`.env` está excluido de Git. La contraseña de SQL Server debe cumplir sus requisitos de complejidad.

## Comandos útiles

Ver los logs:

```powershell
docker compose logs -f
```

Detener conservando bases y colas:

```powershell
docker compose down
```

Detener y eliminar también todos los datos persistidos:

```powershell
docker compose down -v
```

Después de usar `down -v`, el siguiente `up` volverá a crear las dos bases desde `database/init.sql`.

## Subir a GitHub

Desde esta misma carpeta:

```powershell
git init
git add .
git commit -m "Dockeriza microservicios de clínica"
git branch -M main
git remote add origin https://github.com/USUARIO/NOMBRE-REPOSITORIO.git
git push -u origin main
```

Primero cree un repositorio vacío en GitHub, reemplace la URL por la suya y no agregue otro README desde GitHub.

## Solución de problemas

- Si un puerto está ocupado, cierre el programa o contenedor que lo utiliza y repita el comando de inicio.
- Si cambia credenciales después del primer inicio, ejecute `docker compose down -v` y vuelva a levantar el proyecto.
- Si una descarga o compilación falla, revise `docker compose logs` y confirme que Docker Desktop tenga conexión a Internet.
