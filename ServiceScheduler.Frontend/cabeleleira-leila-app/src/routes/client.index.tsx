import { createFileRoute, redirect } from "@tanstack/react-router";

export const Route = createFileRoute("/client/")({
  beforeLoad: () => {
    throw redirect({ to: "/client/book" });
  },
});
