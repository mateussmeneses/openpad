# 🧪 Roteiro de Homologação Manual — OpenPad

Este documento descreve os procedimentos de teste manual para validação do **OpenPad** em ambiente Windows x64 limpo com controle físico e aplicação de teste neutra.

---

## 🎮 Cenário 1: Detecção e Visualização de Controles

| Passo | Ação | Resultado Esperado | Status |
|:---:|---|---|:---:|
| 1.1 | Conectar controle Xbox / DualSense / Pro Controller via cabo USB ou Bluetooth. | O controle é detectado imediatamente pelo SDL3 e exibido na lista de dispositivos. | ⏳ A Homologar |
| 1.2 | Pressionar botões e mover analógicos na janela do OpenPad. | Os indicadores visuais da interface respondem em tempo real refletindo o estado dos eixos e botões. | ⏳ A Homologar |
| 1.3 | Desconectar o cabo USB do controle. | O status do dispositivo muda para "Desconectado" sem travar o aplicativo. | ⏳ A Homologar |

---

## ⌨️ Cenário 2: Remapeamento e Injeção de Teclado/Mouse

| Passo | Ação | Resultado Esperado | Status |
|:---:|---|---|:---:|
| 2.1 | Abrir o Bloco de Notas (Notepad) ou um testador de teclado web (keyboard-test.space). | A aplicação neutra de teste fica focada ao lado do OpenPad. | ⏳ A Homologar |
| 2.2 | Criar perfil com Botão A -> Tecla de Espaço, Botão B -> Tecla 'E'. | Mapeamentos são exibidos na interface. | ⏳ A Homologar |
| 2.3 | Clicar em **Armar (ARM)** no OpenPad. | Indicador muda para ARMED (verde). | ⏳ A Homologar |
| 2.4 | Pressionar Botão A no controle. | O caractere de espaço é inserido no Bloco de Notas. | ⏳ A Homologar |
| 2.5 | Pressionar e segurar Botão A e em seguida pressionar Botão B. | Ambos os inputs são enviados e reconhecidos simultaneamente. | ⏳ A Homologar |
| 2.6 | Soltar Botão A enquanto mantém Botão B. | Espaço é liberado e 'E' permanece pressionado. | ⏳ A Homologar |

---

## 🛑 Cenário 3: Desarme de Emergência e Desconexão

| Passo | Ação | Resultado Esperado | Status |
|:---:|---|---|:---:|
| 3.1 | Com o motor armado, pressionar e manter pressionado o Botão A. | A tecla permanece acionada. | ⏳ A Homologar |
| 3.2 | Pressionar o combo físico de emergência **Back + Start** no controle (ou clicar em Desarmar). | O motor desarma imediatamente e a tecla segurada é liberada pelo SO. | ⏳ A Homologar |
| 3.3 | Armar novamente, segurar o Botão A e puxar o cabo do controle. | A desconexão é detectada e a tecla é imediatamente liberada, prevenindo tecla presa. | ⏳ A Homologar |

---

## 👥 Cenário 4: Multi-Controle (Dois Jogadores)

| Passo | Ação | Resultado Esperado | Status |
|:---:|---|---|:---:|
| 4.1 | Conectar Controle 1 e Controle 2 simultaneamente. | Ambos os slots são listados no OpenPad. | ⏳ A Homologar |
| 4.2 | Ambos pressionam o Botão A (mapeado para Espaço). | Apenas um evento de tecla é enviado para o sistema operacional. | ⏳ A Homologar |
| 4.3 | Jogador 1 solta o Botão A enquanto Jogador 2 continua segurando. | A tecla de espaço **continua** pressionada para o Jogador 2. | ⏳ A Homologar |
| 4.4 | Jogador 2 desconecta o controle. | Apenas as teclas do Jogador 2 são liberadas; Jogador 1 continua operando normalmente. | ⏳ A Homologar |
