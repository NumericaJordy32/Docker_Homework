# Memoria de comandos de Azure

Complete este archivo con los comandos realmente ejecutados, sin secretos.

1. `az login` y `az account show`
2. Crear Resource Group y Azure Container Registry.
3. Construir, etiquetar y publicar imágenes en ACR.
4. Crear Azure SQL, las bases `PacientesDB` y `HistorialClinicoDB` y regla de firewall. Ejecutar `database/azure-pacientes-init.sql` en la primera y `database/azure-historial-init.sql` en la segunda.
5. Crear Container Apps Environment y desplegar OAuthJWT, Pacientes, Historial, RabbitMQ y Gateway.
6. Configurar variables de entorno, cadenas de conexión y rutas.
7. Probar `/oauth/token` y los endpoints protegidos a través del Gateway.
8. Tras la revisión: `az group delete --name <RESOURCE_GROUP> --yes --no-wait`.
