"use client";

import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { testsApi } from "@/lib/admin/tests-api";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from "@/components/ui/dialog";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";

export function AssignTestDialog({
  excludeTestIds,
  onAssign,
}: {
  excludeTestIds: string[];
  onAssign: (testId: string) => Promise<void>;
}) {
  const [open, setOpen] = useState(false);
  const [selectedTestId, setSelectedTestId] = useState<string>("");
  const [pending, setPending] = useState(false);

  const { data } = useQuery({
    queryKey: ["admin", "tests", "published-for-assign"],
    queryFn: () => testsApi.list({ status: "PUBLISHED", pageSize: 100 }),
    enabled: open,
  });

  const options = (data?.items ?? []).filter((t) => !excludeTestIds.includes(t.testId));

  async function handleAssign() {
    if (!selectedTestId) return;
    setPending(true);
    try {
      await onAssign(selectedTestId);
      setOpen(false);
      setSelectedTestId("");
    } finally {
      setPending(false);
    }
  }

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <Button type="button" variant="outline" size="sm" onClick={() => setOpen(true)}>
        Gán bộ đề
      </Button>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Gán bộ đề cho kỳ thi</DialogTitle>
          <DialogDescription>
            Chỉ hiển thị các Test đã ở trạng thái Đã publish (chưa gán cho kỳ thi này).
          </DialogDescription>
        </DialogHeader>

        {options.length === 0 ? (
          <p className="text-sm text-muted">
            Không có bộ đề nào đã publish và chưa gán. Publish một bộ đề trước.
          </p>
        ) : (
          <Select value={selectedTestId} onValueChange={setSelectedTestId}>
            <SelectTrigger>
              <SelectValue placeholder="Chọn bộ đề…" />
            </SelectTrigger>
            <SelectContent>
              {options.map((test) => (
                <SelectItem key={test.testId} value={test.testId}>
                  {test.testCode} — {test.testName}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        )}

        <DialogFooter>
          <Button type="button" variant="outline" onClick={() => setOpen(false)}>
            Huỷ
          </Button>
          <Button
            type="button"
            variant="teal"
            disabled={!selectedTestId || pending}
            onClick={handleAssign}
          >
            {pending ? "Đang gán…" : "Gán"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
