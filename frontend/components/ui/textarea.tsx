import * as React from "react";
import { cn } from "@/lib/utils";

const Textarea = React.forwardRef<HTMLTextAreaElement, React.TextareaHTMLAttributes<HTMLTextAreaElement>>(
  ({ className, ...props }, ref) => {
    return (
      <textarea
        ref={ref}
        className={cn(
          "flex min-h-24 w-full rounded-sm border border-border bg-paper-raised px-3.5 py-2.5 text-sm text-ink placeholder:text-muted",
          "transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-teal focus-visible:border-teal",
          "disabled:cursor-not-allowed disabled:opacity-50",
          className
        )}
        {...props}
      />
    );
  }
);
Textarea.displayName = "Textarea";

export { Textarea };
