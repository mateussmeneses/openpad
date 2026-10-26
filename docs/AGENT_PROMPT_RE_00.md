# PROMPT REA 00 — OpenPad / investigação orientada a evidências

Atue como analista sênior de engenharia reversa e arquiteto C#/.NET, com REA disponível como MCP **se a instalação local estiver saudável**.

1. Leia `AGENTS.md`, `docs/PRD.md`, `docs/ARCHITECTURE.md`, `docs/REA_RESEARCH_PLAN.md` e `../REA_SETUP_WINDOWS.md` (se o monorepo completo estiver presente).
2. Verifique quais ferramentas REA o cliente efetivamente expõe. Não invente nomes de ferramentas; prefira `binary_session` para descobrir disponibilidade se presente. Se REA estiver indisponível, registre a causa e continue pela observação de comportamento permitida e documentação pública.
3. Primeiro objetivo: especificar, com evidência, estados de entrada e efeitos de desarmar/remover perfil/desconectar controle. Investigue a referência reWASD somente em instalação autorizada; não copie código nem contorne a licença.
4. Separe relatório em OBSERVADO / INFERIDO / DESCONHECIDO. Para análise estática, referencie evidência e limites. Não confunda hipótese sobre função interna com comportamento comprovado.
5. Entregue `docs/research/openpad-behavior-matrix.md` com pelo menos 6 cenários, testes esperados e características desejáveis distintas do produto de referência.
6. A partir dessa especificação, implemente apenas testes e interfaces próprias no OpenPad (mantendo código proprietário fora do workspace de implementação). Nada de driver, hook ou input armado por padrão.
7. Rode testes disponíveis; documente comandos, sucesso ou bloqueio real. Não marque como testado algo não executado.

Faça alterações concretas no repositório existente sem reconstruir a solution. Conclua com evidências, lacunas e próximo incremento verificável.
