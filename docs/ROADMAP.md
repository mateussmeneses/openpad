# 🗺️ Roadmap Realista de Produto — OpenPad

Este documento define os marcos de evolução técnica e de produto do **OpenPad**, priorizando a estabilidade, a experiência do jogador e a fidelidade de controle no Windows.

---

## 🎯 Marco 1 — Estabilização do Core e MVP Portátil (Concluído)
- [x] Detecção de gamepads físicos via SDL3 Gamepad API.
- [x] Injeção de teclado e mouse via Win32 `SendInput()` com validação de retorno.
- [x] Isolamento completo entre múltiplos controles com contagem de referências.
- [x] Homologação automatizada dos 10 passos do ciclo de vida do remapeamento.
- [x] Atalho físico de emergência no gamepad (Back + Start).
- [x] Empacotamento portátil x64 com cópia nativa da `SDL3.dll`.
- [x] 182 testes unitários e de integração com 100% de aprovação.

---

## 🎮 Marco 2 — Homologação de Campo e Interface Moderna (Próximo)
- [ ] Homologação manual com controle físico Xbox Series, DualSense e Nintendo Switch Pro.
- [ ] Atalho configurável de emergência pelo teclado físico (ex: `Ctrl + Shift + F12`).
- [ ] Visualizador de resposta analógica com visualização em gráfico dos eixos e deadzones.
- [ ] Geração de release zip autocontida com checksum SHA-256 no GitHub Releases.

---

## 🚀 Marco 3 — Recursos Avançados e Drivers Virtuais
- [ ] Pesquisa e integração com driver assinado ViGEmBus para saída virtual de controle Xbox 360 / DualShock 4.
- [ ] Suporte a curvas cinemáticas avançadas e deadzones em formato circular/axial personalizáveis na interface.
- [ ] Alternância automática de perfil por processo ativo em primeiro plano.
