export function AudioBlock({ audioUrl }: { audioUrl: string }) {
  return (
    <div className="rounded-sm border border-border bg-ink/[0.03] p-4">
      <audio controls preload="none" src={audioUrl} className="w-full">
        Trình duyệt của bạn không hỗ trợ phát audio.
      </audio>
    </div>
  );
}
