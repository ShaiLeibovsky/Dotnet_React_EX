export const FieldError = ({ id, children }: { id: string; children: string }) => (
    <p id={id} role="alert" className="text-destructive text-sm">
        {children}
    </p>
)
