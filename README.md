# Capstone\_grupo\_4





Comandos GIT



🔹 1. Configuración inicial (solo la primera vez)

git config --global user.name "Tu Nombre"

git config --global user.email "tu\_correo@example.com"



🔹 2. Clonar el repositorio remoto



(Para ambos, si todavía no tienen el repo en su PC)



git clone URL\_DEL\_REPOSITORIO





Ejemplo:



git clone https://github.com/usuario/proyecto.git



🔹 3. Crear y cambiar a una rama propia



(para no trabajar directamente en main)



git checkout -b nombre-de-tu-rama





Ejemplo:



git checkout -b feature/login



🔹 4. Agregar cambios

git add .





(o también archivo por archivo: git add archivo.cshtml)



🔹 5. Hacer commit con mensaje

git commit -m "Descripción clara del cambio"



🔹 6. Subir tu rama al repositorio remoto

git push origin nombre-de-tu-rama



🔹 7. Traer cambios de tu compañero



Primero, cambiar a main:



git checkout main





Actualizar desde el remoto:



git pull origin main



🔹 8. Integrar cambios de main a tu rama



(Para mantener tu rama actualizada con lo que haga tu compañero)



git checkout nombre-de-tu-rama

git merge main



🔹 9. Resolver conflictos (si los hay)



Si Git marca conflictos:



Abrir los archivos con conflictos



Editar manualmente las secciones



Guardar



Luego:



git add archivo\_conflictivo

git commit



🔹 10. Pull Request / Merge



En GitHub/GitLab/Bitbucket:



Creas un Pull Request (PR) desde tu rama hacia main



Tu compañero revisa y aprueba



Se hace el merge a main

