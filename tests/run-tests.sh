#!/usr/bin/env bash
#
# Ejecuta la suite de pruebas de EDUSIS.
# Contrato: specs/001-automated-test-suite/contracts/test-execution.md (§2, §3).
#
# Uso:
#   ./tests/run-tests.sh --rapidas     # sólo Categoria=Unidad (dominio + aplicación, sin BD)
#   ./tests/run-tests.sh --todas       # todas las categorías aplicables al entorno
#   ./tests/run-tests.sh --cobertura   # --todas + reporte HTML de cobertura
#   ./tests/run-tests.sh               # = --todas
#
# - Se puede ejecutar desde cualquier directorio: resuelve la raíz por git.
# - Propaga tal cual el código de salida de `dotnet test` (0 = ok, 1 = falló algo).
# - Cualquier argumento extra se reenvía a `dotnet test` (p. ej. -v n, --filter ...).

set -euo pipefail

RAIZ="$(git rev-parse --show-toplevel 2>/dev/null || true)"
if [ -z "$RAIZ" ] || [ ! -f "$RAIZ/EDUSIS.sln" ]; then
	echo "error: ejecutá este script desde dentro del checkout de EDUSIS." >&2
	exit 1
fi
SLN="$RAIZ/EDUSIS.sln"

modo="${1:---todas}"
if [ "$#" -gt 0 ]; then
	shift
fi

case "$modo" in
	--rapidas)
		exec dotnet test "$SLN" --filter "Categoria=Unidad" "$@"
		;;
	--todas)
		exec dotnet test "$SLN" "$@"
		;;
	--cobertura)
		SALIDA_COBERTURA="$RAIZ/tests/CoverageReport"
		codigo=0
		dotnet test "$SLN" --collect:"XPlat Code Coverage" "$@" || codigo=$?

		if command -v reportgenerator >/dev/null 2>&1; then
			reportgenerator \
				"-reports:$RAIZ/tests/**/TestResults/**/coverage.cobertura.xml" \
				"-targetdir:$SALIDA_COBERTURA" \
				"-reporttypes:Html"
			echo "Reporte de cobertura: $SALIDA_COBERTURA/index.html"
		else
			echo "AVISO: 'reportgenerator' no está instalado; se omite el HTML de cobertura." >&2
			echo "       Instalar con: dotnet tool install -g dotnet-reportgenerator-globaltool" >&2
		fi

		exit "$codigo"
		;;
	*)
		# El primer argumento no es un modo conocido: se trata como argumento de --todas.
		exec dotnet test "$SLN" "$modo" "$@"
		;;
esac
