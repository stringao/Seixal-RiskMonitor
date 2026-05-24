import Link from "next/link";

export default function Home() {
  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-950 via-slate-900 to-emerald-950 flex flex-col items-center justify-center px-6">
      <main className="text-center max-w-2xl">
        <div className="mb-4 inline-flex items-center gap-2 rounded-full border border-emerald-500/30 bg-emerald-500/10 px-4 py-1.5 text-sm text-emerald-400">
          <span className="h-2 w-2 rounded-full bg-emerald-400 animate-pulse"></span>
          {"Monitorização Ativa"}
        </div>

        <h1 className="text-5xl font-bold tracking-tight text-white mb-4">
          {"SeixalRisk"}
          <span className="text-emerald-400">{" Monitor"}</span>
        </h1>

        <p className="text-lg text-slate-400 mb-10 leading-relaxed">
          Plataforma de monitorização de riscos geográficos para o Concelho do Seixal.
          Análise espacial em tempo real com inteligência artificial.
        </p>

        <Link
          href="/login"
          className="inline-flex h-12 items-center justify-center rounded-lg bg-emerald-500 px-8 text-base font-semibold text-white transition-colors hover:bg-emerald-400"
        >
          Entrar
        </Link>

        <div className="mt-16 grid grid-cols-3 gap-6 text-center">
          <div>
            <div className="text-2xl font-bold text-white">PostGIS</div>
            <div className="text-sm text-slate-500">Análise Espacial</div>
          </div>
          <div>
            <div className="text-2xl font-bold text-white">LLM</div>
            <div className="text-sm text-slate-500">Insights IA</div>
          </div>
          <div>
            <div className="text-2xl font-bold text-white">Tempo Real</div>
            <div className="text-sm text-slate-500">Dados Portugueses</div>
          </div>
        </div>
      </main>
    </div>
  );
}
