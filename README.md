# EnergyStressTestRunner
Executor do Teste de Stress, Alavancagem e Cálculo de Garantias para Energia da Volt Robotic & Elekto, proposta Abraceel

Copyright © 2021-2022 by Elekto Produtos Financeiros & Volt Robotics

Programa para executar testes de stress em Porfólios de Energia, conforme estudo encomendado pela Abraceel.

Uso didático, não comercial!

## Instalação

Simplesmente descompacte o arquivo zip num diretório qualquer de uma máquina executanto Windows (8 ou superior). O programa chama-se `StressTestRunner.exe` o programa de cálculo de alavancagem, mantenha-o junto dos arquivos e diretórios auxiliares enviados.

O programa que calcula as garantias é um programa de linha de comando, no mesmo diretório, chamado `StressTestRunnerCli.exe`. 

É requerido que a máquina possua o .Net Framework 4.8, que qualquer Windows relativamente atualizado possuirá. Se ao executar o programa pela primeira vez ele retornar um erro requerendo o .Net 4.8, baixe-o e instale-o a partir da Microsoft em https://dotnet.microsoft.com/download/dotnet-framework/net48 (clique no web installer do Runtime).

## Execução do Cálculo da Alavancagem

O programa é simples de usar: basta selecionar uma planilha Excel, com formato especifico (exemplos no sub-diretório "Exemplos), contendo as posições de energia em datas determinadas e clicar em Executar. Para posições complexas é possível que a execução demore alguns poucos segundos, a maior parte consumidos escrevendo o Excel de resultados, seja paciente.

O programa irá ler as posições informadas, e gerar os trades (a mercado) necessários para gerar as posições reportadas em cada data. Se nem todos os dias úteis forem informados, o programa irá interpolar compras ou vendas a cada dia útil para transformar as posições conforme necessário.

Conforme proposta da Volt/Elekto somente as posições são necessárias, sem necessidade de informar os trades exatos feitos, e seus preços acordados. As planilhas de instrução contém detalhes de preenchimento.

No mesmo diretório do arquivo de posições o programa criára arquivos texto de evidência e uma planilha (o mesmo nome do arquivo de entrada, com o sufixo "Report") contendo os resultados dos cálculos.

O programa não é limitado por um horizonte de análise, depende apenas de informar, ou não, as posições até um horizonte espeçifico (M+3, M+6 etc); mas as curvas de stress são calibradas para um portfólio de duração média de M+3, embora sejam parâmetros de calibração que não são tão relevantes.

O stress padrão, em tela, de 47%, corresponde ao corte de 99% no prazo de M+3, numa janela de 4 anos finalizando em 2021-06.

Este programa não se comunica pela Internet, nenhum dado é baixado de servidor algum, nenhuma telemetria é coletada e enviada, nenhuma posição ou relatório é extraído e enviado. Executa totalmente na máquina local.

## Curvas

No subdiretório "Data" o arquivo Energia.txt contém as curvas de energia calculadas entre 2017-01-06 e 2021-06-11, e são exatamente as mesmas usadas no estudo apresentado para a Abraceel. Caso possua outras curvas elas podem ser colocadas nesse mesmo arquivo, respeitando o layout:

* A 1ª linha é um header e é ignorada
* Cada linha contém 3 campos, separados por tabulação
* A ordem dos campos é:
	* "data de referência": a data de fechamento da curva, no formato yyyy-MM-dd; 
	* "data de vértice": a data de inflexões, que normalmente corresponde as datas de pagamento dos contratos, no formato yyyy-MM-dd; 
	* "valor da energia": em R$/Mwh, na formatação inglesa, sem separador de milhar, usando ponto como separador decimal.

## Execução do Cálculo das Garantias

O cálculo das garantias é executado pelo programa `StressTestRunnerCli.exe`. É um programa em linha de comando, e os parâmetros de comando e suas opções podem ser obtidos com `StressTestRunnerCli.exe Run --help`.

## Garantias do Software

Os melhores esforços foram empreendidos para que o programa execute corretamente. No entando nem Elekto e nem Volt Robotics dão qualquer garantia quanto aos resultados e consequencias do uso correto, ou não, do programa. Nenhum prejuizo, de qualquer natureza, poderá ser imputado a Elekto ou a Volt Robotics, pelo uso correto ou não desse programa.

## Compilando o Código

Usamos o Visual Studio 2019. A versão Community será suficiente, assim como o VS Code. Basta abrir a solução e compilar.
