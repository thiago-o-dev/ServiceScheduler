import { createFileRoute } from "@tanstack/react-router";
import { PageHeader } from "@/components/shell/PageHeader";
import { BookingWizard } from "@/components/booking/BookingWizard";

export const Route = createFileRoute("/client/book")({
  head: () => ({ meta: [{ title: "Agendar — Cabeleleira Leila" }] }),
  component: BookPage,
});

function BookPage() {
  return (
    <div className="space-y-8">
      <PageHeader
        title="Agendar"
        description="Escolha seus serviços favoritos, a profissional e o melhor horário."
      />
      <BookingWizard />
    </div>
  );
}
