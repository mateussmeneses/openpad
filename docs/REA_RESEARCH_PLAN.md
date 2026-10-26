# OpenPad — pesquisa com REA antes da implementação

## Alvo de referência

reWASD (instalação oficial, sob licença de teste válida); comparar adicionalmente soluções livres como AntiMicroX. A referência é funcional, **não** fonte de código a incorporar. Não modificar, destravar ou instrumentar drivers fechados para burlar proteções.

## Pergunta #1 para o MVP

O que um programa de remapeamento faz quando uma tecla de saída está pressionada e o usuário desativa o perfil, troca de perfil, desconecta o controle ou fecha o aplicativo?

## Etapa A — comportamento observável

1. Criar ambiente de teste Windows isolado e app neutro que exiba eventos de entrada (ou usar Notepad para o comportamento de digitação, lembrando que Notepad não mostra `keyup`).
2. Abrir software de referência licenciado e executar cenário com botão físico A → tecla Espaço, se disponível na edição testada.
3. Repetir com toggle ativo/inativo, desconexão, fechamento, conflito e troca de perfil.
4. Anotar o que a ferramenta realmente mostrou/fez. Capturar apenas as próprias interações e configurações autorizadas.
5. Para cada comportamento, converter em teste automatizado nosso usando `FakeControllerSource` e `RecordingInputSink`.

## Etapa B — triagem de arquitetura com REA

1. Opcional: `inspect_desktop_application` na pasta instalada, para identificar tecnologias e próximos probes, sem execução.
2. Se houver assembly .NET legítimo para examinar: `inspect_managed_artifact` e `inspect_managed_members` para metadados/CIL (não rodar no binário de produção do OpenPad sem necessidade).
3. Se uma dúvida objetiva exigir binário nativo e Ghidra estiver validado: abrir somente o componente pertinente e analisar fluxos de entrada em modo leitura. Documentar hipóteses, não afirmar que a análise estática prova timing real.
4. Medir latência, deadzone, polling e comportamento de remapeamento **no OpenPad**, em testes próprios. Os resultados devem se apoiar em medições, não em dedução a partir de código comercial.

## O que fica fora do primeiro ciclo

Emulação virtual XInput, esconder dispositivos físicos, filtros globais, drivers fechados, hooks em jogos, anti-cheat, bypass de licenças e recursos premium bloqueados no app de referência.

## Critérios de saída

- Matriz de comportamento para ao menos 6 cenários, com evidências e limites declarados.
- Pelo menos 6 testes unitários de transições de estado no projeto próprio.
- `AGENTS.md` e `PRD.md` atualizados apenas com comportamentos validados.
- Nenhum trecho decompilado, DLL comercial ou asset fechado versionado no GitHub.
