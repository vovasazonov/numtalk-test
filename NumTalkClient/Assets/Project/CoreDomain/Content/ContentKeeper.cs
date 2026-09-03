namespace Project.CoreDomain.Content
{
    internal class ContentKeeper<T> : IContentKeeper<T>
    {
        private readonly ContentService _contentService;
        private readonly string _contentId;
        private bool _isDisposed;

        public T Value { get; }

        public ContentKeeper(ContentService contentService, string contentId, T value)
        {
            _contentService = contentService;
            _contentId = contentId;
            Value = value;
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _contentService.Unload(_contentId);
                _isDisposed = true;
            }
        }
    }
}