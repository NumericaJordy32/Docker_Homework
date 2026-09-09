# Memoria de comandos de Azure

Registro de pasos ejecutados, sin secretos.

1. Se verificó la suscripción con `az account show` desde Azure Cloud Shell.
2. Se mantuvieron el Resource Group `rg-clinica-aa-jordy-202609`, el ACR `acrclinicajordy202609.azurecr.io`, las dos bases Azure SQL y las cinco Container Apps ya creadas.
3. Se configuraron referencias a secretos de Container Apps para cadenas SQL, RabbitMQ, usuarios OAuth y la misma `Jwt__Key` en OAuthJWT, Pacientes e Historial. No se registran valores en este archivo.
4. Se configuró RabbitMQ con FQDN interno y puerto TCP 5672 en Pacientes e Historial.
5. Se cambió el mínimo de réplicas a 1 en OAuthJWT, Pacientes, Historial y Gateway para eliminar el escalado a cero y el arranque en frío.
6. Se actualizó el Gateway para usar los FQDN HTTPS públicos de Pacientes, Historial y OAuthJWT como destinos.
7. Validación pública: `/health` del Gateway devolvió 200; `/pacientes` y `/historiales` devolvieron 401 sin token; `/oauth/token` llegó a OAuth y devolvió 400 al enviar un cuerpo vacío.
8. `az acr build` fue rechazado por `TasksOperationsNotAllowed`; como alternativa, GitHub Actions compiló y publicó las imágenes con la etiqueta `072d5f7`.
9. Pacientes e Historial se actualizaron manualmente desde Azure Container Apps a `acrclinicajordy202609.azurecr.io/clinica/<servicio>:072d5f7`. Las dos revisiones finalizaron correctamente.
10. El paso automático `az containerapp update` de GitHub Actions aún requiere permisos adicionales para el principal guardado en `AZURE_CREDENTIALS`; no se incluyen credenciales en este archivo.
11. Tras la revisión: `az group delete --name <NOMBRE_RESOURCE_GROUP> --yes --no-wait`.

## Preparación local para la grabación

Los siguientes comandos no imprimen ni guardan secretos. El archivo `.env` permanece local e ignorado por Git.

```powershell
# Desde la raíz del repositorio
docker compose up --build -d
docker compose ps
docker compose logs --tail 50 api-gateway pacientes-api historial-clinico-api rabbitmq

# Pestañas para la demostración local
Start-Process http://localhost:5100
Start-Process http://localhost:5101/swagger
Start-Process http://localhost:5102/swagger
Start-Process http://localhost:5103/swagger
Start-Process http://localhost:15672
```

## Validación reproducible por Gateway

```powershell
$login = Invoke-RestMethod -Method Post http://localhost:5100/oauth/token `
  -ContentType 'application/json' `
  -Body (@{ usuario = '<OAUTH_USER>'; contrasena = '<OAUTH_USER_PASSWORD>' } | ConvertTo-Json)
$headers = @{ Authorization = "Bearer $($login.token)" }

# Se espera 401 sin encabezado y 200 con JWT.
Invoke-WebRequest http://localhost:5100/pacientes
Invoke-RestMethod http://localhost:5100/pacientes -Headers $headers
Invoke-RestMethod http://localhost:5100/historiales -Headers $headers
```

No ejecutar `Get-Content .env`, no pegar el JWT completo ni abrir archivos de claves durante la grabación. Use variables y marcadores como `<OAUTH_USER>` en toda evidencia entregable.
