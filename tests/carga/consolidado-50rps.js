import http from 'k6/http';
import { check, fail, sleep } from 'k6';
import { textSummary } from 'https://jslib.k6.io/k6-summary/0.1.0/index.js';

const enderecoDoGateway = __ENV.GATEWAY_URL || 'http://gateway:8080';
const enderecoDoKeycloak = __ENV.KEYCLOAK_URL || 'http://keycloak:8080';
const usuarioDoComercianteDeCarga = __ENV.USUARIO || 'loja.praia';
const senhaDoComercianteDeCarga = __ENV.SENHA || 'Verity@2026!';
const taxaDeErroMaxima = Number(__ENV.TAXA_DE_ERRO_MAXIMA || '0.05');
const nomeDoCenario = __ENV.CENARIO || 'consolidado-50rps';

const cenarioDeLeitura = 'leitura_do_consolidado';
const diasDeHistorico = 90;
const lancamentosPorLoteDePreparacao = 30;
const umDiaEmMilissegundos = 24 * 60 * 60 * 1000;
const deslocamentoDoFusoDeSaoPaulo = -3 * 60 * 60 * 1000;

export const options = {
  setupTimeout: '3m',
  scenarios: {
    [cenarioDeLeitura]: {
      executor: 'constant-arrival-rate',
      rate: 50,
      timeUnit: '1s',
      duration: '2m',
      preAllocatedVUs: 50,
      maxVUs: 200,
    },
  },
  thresholds: {
    [`http_req_failed{scenario:${cenarioDeLeitura}}`]: [`rate<${taxaDeErroMaxima}`],
    [`http_req_duration{scenario:${cenarioDeLeitura}}`]: ['p(95)<200', 'p(99)<500'],
    dropped_iterations: ['count<1'],
  },
};

function diaDeslocado(dias) {
  return new Date(Date.now() + deslocamentoDoFusoDeSaoPaulo + dias * umDiaEmMilissegundos).toISOString().slice(0, 10);
}

function obterTokenDoComerciante() {
  const resposta = http.post(
    `${enderecoDoKeycloak}/realms/verity/protocol/openid-connect/token`,
    {
      grant_type: 'password',
      client_id: 'verity-dev',
      username: usuarioDoComercianteDeCarga,
      password: senhaDoComercianteDeCarga,
    },
  );

  if (resposta.status !== 200)
    fail(`Não foi possível obter o token no Keycloak: ${resposta.status} ${resposta.body}`);

  return resposta.json('access_token');
}

function montarLancamentoDoDia(dataCompetencia, indiceDoDia, identificadorDaExecucao, cabecalhosAutenticados) {
  const ehDebito = indiceDoDia % 3 === 0;

  return [
    'POST',
    `${enderecoDoGateway}/lancamentos`,
    JSON.stringify({
      tipo: ehDebito ? 'Debito' : 'Credito',
      valor: ehDebito ? 75.5 : 120 + indiceDoDia,
      dataCompetencia,
      descricao: 'Massa de dados do teste de carga',
    }),
    {
      headers: {
        ...cabecalhosAutenticados,
        'Content-Type': 'application/json',
        'Idempotency-Key': `carga-${identificadorDaExecucao}-${indiceDoDia}`,
      },
    },
  ];
}

function registrarHistoricoDeLancamentos(diasDoHistorico, cabecalhosAutenticados) {
  const identificadorDaExecucao = Date.now();
  const lancamentos = diasDoHistorico.map((dataCompetencia, indiceDoDia) =>
    montarLancamentoDoDia(dataCompetencia, indiceDoDia, identificadorDaExecucao, cabecalhosAutenticados));

  for (let inicioDoLote = 0; inicioDoLote < lancamentos.length; inicioDoLote += lancamentosPorLoteDePreparacao) {
    const respostas = http.batch(lancamentos.slice(inicioDoLote, inicioDoLote + lancamentosPorLoteDePreparacao));
    const todosRegistrados = respostas.every((resposta) => resposta.status === 201);

    if (!todosRegistrados)
      fail(`Falha ao preparar o histórico: ${respostas.map((resposta) => resposta.status).join(', ')}`);

    sleep(1);
  }
}

export function setup() {
  const token = obterTokenDoComerciante();
  const cabecalhosAutenticados = { Authorization: `Bearer ${token}` };
  const diasDoHistorico = Array.from({ length: diasDeHistorico }, (_, indice) => diaDeslocado(-indice));

  registrarHistoricoDeLancamentos(diasDoHistorico, cabecalhosAutenticados);

  return { cabecalhosAutenticados, diasDoHistorico };
}

export default function (dadosDaPreparacao) {
  const { cabecalhosAutenticados, diasDoHistorico } = dadosDaPreparacao;
  const diaConsultado = diasDoHistorico[Math.floor(Math.random() * diasDoHistorico.length)];

  const resposta = http.get(`${enderecoDoGateway}/consolidado/diario/${diaConsultado}`, {
    headers: cabecalhosAutenticados,
    tags: { name: 'GET /consolidado/diario/{data}' },
  });

  check(resposta, {
    'status 200': (respostaRecebida) => respostaRecebida.status === 200,
    'saldo do dia consultado': (respostaRecebida) => respostaRecebida.status === 200 && respostaRecebida.json('data') === diaConsultado,
  });
}

export function handleSummary(resumo) {
  const instante = new Date().toISOString().replace(/[:.]/g, '-');
  const arquivoBase = `/scripts/resultados/${nomeDoCenario}-${instante}`;

  return {
    stdout: textSummary(resumo, { indent: ' ', enableColors: true }),
    [`${arquivoBase}.txt`]: textSummary(resumo, { indent: ' ', enableColors: false }),
    [`${arquivoBase}.json`]: JSON.stringify(resumo, null, 2),
  };
}
