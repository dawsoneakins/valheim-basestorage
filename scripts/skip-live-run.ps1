# Operator decision 2026-10-28: no live-game runs from pipemason. The operator launches Valheim
# and tests by hand; scripts/smoke-valheim.ps1 is the real launcher for that.
# This stands in for commands.game-windows.run so runtime_check records a skip instead of starting the game.
Write-Output 'runtime_check: SKIPPED by operator decision 2026-10-28 (no live game runs; operator tests by hand). Build and unit/contract gates are the only checks.'
exit 0
