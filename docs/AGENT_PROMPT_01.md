# PROMPT DE EXECUÇÃO 01 — OpenPad

Você é engenheiro sênior .NET/WPF especializado em SDL3, APIs Win32 e input de baixa latência. Este repositório já foi preparado por `bootstrap.ps1`.

1. Leia `AGENTS.md`, `docs/PRD.md`, `docs/ARCHITECTURE.md`, `docs/ROADMAP.md` e todos os projetos existentes. Não recrie a solution.
2. Implemente a Sprint 1 como programa Windows realmente utilizável. Detecte controles SDL3, mostre botões/eixos em UI moderna WPF e permita mapear um botão a uma tecla ou botão do mouse.
3. Construa um motor de mapeamento independente da UI, testado com `FakeControllerSource` e `RecordingInputSink`. Proteja o ciclo de vida e a thread UI. Trate desconexão e exceções.
4. Implemente ativação/desativação explícita, **desativado por padrão**. Ao desativar/desconectar/trocar perfil/fechar, libere todos os inputs sintéticos que estiverem pressionados.
5. Implemente persistência de perfis em JSON versionado, com leitura/escrita segura, validação e teste de round-trip.
6. Use `SendInput` para teclado/mouse. Não implemente driver virtual, hooks/injeção em jogo, serviço Windows ou recursos que exijam admin.
7. Verifique previamente a API SDL3 e licença. Configure DLL nativa x64 na saída de build usando mecanismo reproduzível e documente a origem. Se dispositivo real não estiver presente, use fonte falsa só nos testes e mostre status honesto no app.
8. Rode `dotnet build`, `dotnet test`, `dotnet run --project src/OpenPad.App/OpenPad.App.csproj` quando estiver no Windows compatível. Não afirme compatibilidade testada com gamepads sem controlar um aparelho real.
9. Feche com arquivos alterados, comandos executados, resultado dos testes, documentação de execução, limitações e próximos passos. Nada de botões decorativos ou exemplos que fingem estar conectados na UI de produção.

Faça escolhas locais razoáveis e documente. NÃO importe código comercial do reWASD ou copie interfaces proprietárias.
