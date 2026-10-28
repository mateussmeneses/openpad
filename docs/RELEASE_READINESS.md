# 🚀 Critérios de Prontidão para Distribuição (Release Readiness) — OpenPad

**Data de Avaliação:** 28/10/2026  
**Versão Alvo:** Windows x64 MVP Portable  
**Branch:** `stabilization/windows-input-reliability`

---

## 📋 Checklist de Prontidão

| Critério | Meta | Evidência / Status | Avaliação |
|---|---|---|:---:|
| **Compilação Release** | 0 avisos, 0 erros | `dotnet build --warnaserror` conclui com 0 avisos e 0 erros. | ✅ APROVADO |
| **Suíte de Testes Automatizados** | 100% de sucesso | 186 testes unitários e de ciclo de vida aprovados no NUnit. | ✅ APROVADO |
| **Validação e Resiliência Win32** | SendInput e liberações pendentes | Retorno inteiro validado, tracking de `PendingReleaseKeys` e `RetryPendingReleases()`. | ✅ APROVADO |
| **Isolamento por Entrada Física** | Zero contaminação cruzada | Entradas físicas rastreadas individualmente por controle com contadores de referência. | ✅ APROVADO |
| **Isolamento de Atalho de Arme** | Combo `Back + Start` por controle | Detecção de combo desacoplada por dispositivo sem interferência mútua. | ✅ APROVADO |
| **Homologação do Ciclo de Vida** | 10 passos determinísticos | Teste automatizado cobrindo conexão, desarme, desconexão, troca de perfil e dispose. | ✅ APROVADO |
| **Empacotamento Portátil Estrito** | Falha de compilação se `SDL3.dll` ausente | Verificação estrita no MSBuild e empacotamento ZIP + SHA-256 no GitHub Actions. | ✅ APROVADO |
| **Documentação Transparente** | Diferenciação real vs experimental | `README.md` e `docs/ARCHITECTURE.md` reclassificados. | ✅ APROVADO |
| **Homologação em Hardware Físico** | Testes manuais com gamepads reais | Pendente execução física por usuário / jogador no ambiente de destino. | ⏳ PENDENTE |

---

## 🛡️ Riscos Residuais e Recomendações
1. **Jogos com Anti-Cheat Kernel (Easy Anti-Cheat / BattlEye / Vanguard):** Como o OpenPad utiliza `SendInput()` padrão do usuário, jogos competitivos que bloqueiam injeções sintéticas de mouse/teclado podem ignorar os comandos sem permissões elevadas. Recomenda-se executar o OpenPad como Administrador quando necessário.
2. **Distribuição Portable:** O pacote publicado em `publish/` é 100% autocontido e não requer instalador MSI ou drivers adicionais de terceiros.
